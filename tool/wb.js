#!/usr/bin/env node

var async = require('async'),
  fs = require('fs'),
  path = require('path'),
  wb = require('./index');

var argv = require('yargs')
  .usage('Usage: $0 <command> [options] files...')
  .demand(2)
  .command('ls', 'List the files in a bundle')
  .example('$0 ls foo.wb', 'List the files stored in foo.wb')
  .command('decode', 'Decode files from a bundle')
  .example('$0 decode foo.wb', 'Extract files from foo.wb, writing them to the current directory')
  .example('$0 decode foo.wb -h keyboardcat', 'Extract files from foo.wb, decrypting them with the pass "keyboardcat"')
  .example('$0 decode -o /some/dir -x data.json foo.wb', 'Extract data.json from foo.wb and write it to /some/dir')
  .command('encode', 'Encode files into a bundle')
  .example('$0 encode foo.json bar.png', 'Encode foo.json and bar.png end store them in data.wb.png')
  .example('$0 encode foo.json -h keyboardcat', 'Encode foo.json, encrypting the bundle with the pass "keyboardcat"')
  .example('$0 encode foo.json bar.png -o my-bundle.wb', 'Encode foo.json and bar.png and store them in my-bundle.wb')
  .example('$0 encode -a foo.json -o my-bundle.wb', 'Add foo.json the existing bundle my-bundle.wb')
  .options('x', {
    alias: 'extract',
    describe: 'File to extract from a decoded bundle'
  })
  .options('o', {
    alias: 'output',
    describe: 'Output location for encoded bundle or extracted files. Defaults to ./ for decode and ./data.wb for encode'
  })
  .options('a', {
    alias: 'add',
    describe: 'Add files to an existing bundle, instead of creating a new one',
    type: 'boolean'
  })
  .options('k', {
    alias: 'key',
    describe: 'XOR the contents of the bundle using the key provided, providing simple encryption'
  })
  .help('h')
  .alias('h', 'help')
  .check(function(argv) {
    var command = argv._ && argv._[0];
    return command === 'ls' || command === 'decode' || command === 'encode';
  })
  .argv;

var command = argv._.shift(),
  files = argv._;

switch(command) {
  case 'ls':
    ls(files, argv);
    break;

  case 'decode':
    argv.output = argv.output || './';
    decode(files, argv);
    break;

  case 'encode':
    argv.output = argv.output || './data.wb';
    encode(files, argv);
    break;
}

function ls(files, options) {
  var decoder = new wb.Bundle(options.key);
  async.each(files, decoder.load.bind(decoder), function(err) {
    if (err) throw err;

    var files = Object.keys(decoder.data).sort();
    for (var i = 0; i < files.length; i++) {
      console.log("%s\t%s\t%d", files[i], decoder.data[files[i]].type, decoder.data[files[i]].length);
    }
  });
}

function decode(files, options) {
  var decoder = new wb.Bundle(options.key);
  async.each(files, decoder.load.bind(decoder), function(err) {
    if (err) throw err;

    var toExtract, isDir;

    if (options.extract) {
      if (decoder.data[options.extract]) {
        toExtract = [options.extract];
      } else {
        console.error('Error: %s is not in the bundle', options.extract);
        process.exit(1);
      }
    } else {
      toExtract = Object.keys(decoder.data);
    }

    if (fs.existsSync(options.output) && fs.statSync(options.output).isDirectory()) isDir = true;
    if (!isDir && toExtract.length > 1) {
      console.log('%s must be a directory when there is more than one file to extract', options.output);
    }

    async.each(toExtract, function(filename, cb) {
      var output;
      if (isDir) {
        output = path.resolve(options.output, filename);
      } else {
        output = path.resolve(options.output);
      }

      fs.writeFile(output, decoder.read(filename), function(err) {
        if (err) return cb(err);
        console.log('Extracted %s to %s', filename, output);
        cb();
      });
    }, function(err) {
      if (err) throw err;
    });
  });
}

function encode(files, options) {
  getBundle(options.output, options.add, options.key, function(bundle) {
    async.each(files, bundle.addFile.bind(bundle), function(err) {
      if (err) throw err;
      bundle.write(options.output, function(err, length, original) {
        if (err) throw err;
        console.log('Wrote %d bytes to %s. Original %d, compressed %s%', length, options.output, original, (100 * (original - length) / original).toFixed(2));
      });
    });
  });
}

function getBundle(file, add, key, cb) {
  var bundle = new wb.Bundle(key);
  if (add) {
    bundle.load(file, function() {
      cb(bundle);
    });
  } else {
    cb(bundle);
  }
}
