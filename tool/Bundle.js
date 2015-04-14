var fs = require('fs'),
  path = require('path'),
  PNG = require('png-js'),
  nodePng = require('node-png');

function Bundle(key) {
  if (key) {
    if (typeof key !== 'string') {
      throw new TypeError('key should be a string, not ' + typeof key);
    }
    this.key = new Buffer(key);
  }
  this.data = {};
}

// Get an array of files in this bundle
Bundle.prototype.files = function() {
  var self = this;
  return Object.keys(this.data)
    .map(function(name) {
      return self.data[name];
    });
};

// Load a png file and decode it
Bundle.prototype.load = function(filename, cb) {
  var self = this;
  fs.readFile(filename, function(err, data) {
    if (err) return cb(err);
    (new PNG(data)).decode(function(pixels) {
      self.decode(stripAlpha(pixels));
      cb();
    });
  });
};

// Decode a pixel buffer
Bundle.prototype.decode = function(buffer) {
  var files, file, i;

  if (this.key) xor(buffer, this.key);

  // Read headers
  i = 0;
  while(buffer[i] > 4) i++;
  files = parseHeaders(buffer.slice(0, i).toString(), i + 1);

  // Extract files
  for (i = 0; i < files.length; i++) {
    file = files[i];
    file.data = buffer.slice(file.start, file.end);
    this.data[file.name] = file;
  }
};

// Read a file:
Bundle.prototype.read = function(name) {
  if (this.data[name]) {
    return this.data[name].data;
  } else {
    throw new Error(name + ' is not in this bundle');
  }
};

Bundle.prototype.readString = function(name) {
  return this.read(name).toString();
};

Bundle.prototype.readJSON = function(name) {
  return JSON.parse(this.readString(name));
};

// Add a buffer to the bundle
Bundle.prototype.add = function(name, data, type) {
  type = type || path.extname(name).replace(/^\./, '');
  var file = {
    name: name,
    data: data,
    type: type,
    length: data.length
  };
  this.data[file.name] = file;
};

// Read a file and add it to the bundle
Bundle.prototype.addFile = function(name, options, cb) {
  var self = this;
  if (typeof options === 'function') {
    cb = options;
    options = {};
  }

  fs.readFile(name, function(err, data) {
    if (err) return cb(err);
    self.add(options.name || name, data, options.type);
    cb();
  });
};

Bundle.prototype.write = function(filename, cb) {
  var channels = 3,
    buffer = this.toBuffer(),
    pixelCount = Math.ceil(buffer.length / channels),
    width = Math.ceil(Math.sqrt(pixelCount)),
    height = Math.ceil(pixelCount / width),
    png = new nodePng.PNG({width: width, height: height}),
    i, srcIdx, dstIdx, file;

  for (i = 0; i < pixelCount; i++) {
    srcIdx = i * 3;
    dstIdx = i * 4;
    png.data[dstIdx] = buffer[srcIdx];
    png.data[dstIdx + 1] = buffer[srcIdx + 1];
    png.data[dstIdx + 2] = buffer[srcIdx + 2];
    png.data[dstIdx + 3] = 255;
  }

  file = fs.createWriteStream(filename);
  file.on('close', function() {
    cb(null, file.bytesWritten, png.data.length);
  });
  png.pack().pipe(file);
};

// Convert a bundle to a buffer, containing header data and the contents of each file
Bundle.prototype.toBuffer = function() {
  var files = this.files(),
    headers = createHeaders(files),
    length = Buffer.byteLength(headers) + 1 + files.reduce(function(a, b) { return a + b.data.length; }, 0),
    buffer = new Buffer(length),
    start, i, file;

  buffer.write(headers);
  start = Buffer.byteLength(headers) + 1;

  for (i = 0; i < files.length; i++) {
    file = files[i];
    file.length = file.data.length;
    file.start = start;
    file.end = start += file.length;
    file.data.copy(buffer, file.start);
  }

  if (this.key) xor(buffer, this.key);
  return buffer;
};

function parseHeaders(headers, start) {
  return headers.split(';')
    .filter(function(header) { return header.trim() !== ''; })
    .map(function(header) {
      var parts = header.trim().split(','),
        file = {
          name: parts[0],
          type: parts[1],
          length: parseInt(parts[2], 10)
        };

      file.start = start;
      file.end = start += file.length;
      return file;
    });
}

function createHeaders(files) {
  return files
    .map(function (file) {
      return [file.name, file.type, file.data.length].join(',');
    })
    .join(';') + ';';
}

// Copy a pixel buffer into a new buffer, removing the alpha channel
function stripAlpha(src) {
  var dest = new Buffer(src.length * 0.75),
    i, k, l;

  for (i = k = 0, l = src.length; i < l; i++) {
    if ((i + 1) % 4 === 0) continue;
    dest[k] = src[i];
    k++;
  }

  return dest;
}

function xor(buffer, key) {
  for (var i = 0; i < buffer.length; i++) {
    buffer[i] = buffer[i] ^ key[i % key.length];
  }
}

module.exports = Bundle;
