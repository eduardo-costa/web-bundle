var fs = require('fs'),
  PNG = require('png-js');

function Decoder() {
  this.data = {};
}

// Load a png file and decode it
Decoder.prototype.load = function(filename, cb) {
  var self = this;
  fs.readFile(filename, function(err, data) {
    if (err) return cb(err);
    (new PNG(data)).decode(function(pixels) {
      self.decode(pixels);
      cb();
    });
  });
};

// Decode a pixel buffer
Decoder.prototype.decode = function(buffer) {
  var files, file, i;
  buffer = stripAlpha(buffer);

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
Decoder.prototype.read = function(name) {
  return this.data[name].data;
};

Decoder.prototype.readString = function(name) {
  return this.read(name).toString();
};

Decoder.prototype.readJSON = function(name) {
  return JSON.parse(this.readString(name));
};

function parseHeaders(headers, start) {
  return headers.split(';')
    .filter(function(header) { return header.trim() !== '' })
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
};

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

module.exports = Decoder;
