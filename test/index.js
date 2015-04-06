var async = require('async'),
  assert = require('assert'),
  fs = require('fs'),
  wb = require('../tool');

async.series([
  function (cb) {
    var decoder = new wb.Bundle();
    decoder.load('test/data.wb.png', function(err) {
      if (err) return cb(err);
      assert.deepEqual(fs.readFileSync('test/data.json'), decoder.read('data.json'));
      assert.deepEqual(fs.readFileSync('test/portrait.png'), decoder.read('portrait.png'));
      assert.deepEqual(fs.readFileSync('test/xml.dae'), decoder.read('xml.dae'));
      cb();
    });
  },
  function(cb) {
    var encoder = new wb.Bundle();
    async.each(['test/data.json', 'test/portrait.png', 'test/xml.dae'], encoder.addFile.bind(encoder), function(err) {
      if (err) return cb(err);
      encoder.write('test/test.wb.png', function(err) {
        if (err) return cb(err);
        var decoder = new wb.Bundle();
        decoder.load('test/test.wb.png', function(err) {
          if (err) return cb(err);
          assert.deepEqual(fs.readFileSync('test/data.json'), decoder.read('test/data.json'));
          assert.deepEqual(fs.readFileSync('test/portrait.png'), decoder.read('test/portrait.png'));
          assert.deepEqual(fs.readFileSync('test/xml.dae'), decoder.read('test/xml.dae'));
          fs.unlinkSync('test/test.wb.png');
          cb();
        });
      });
    });
  },
  function (cb) {
    var decoder = new wb.Bundle('keyboardcat');
    decoder.load('test/data-encrypted.wb.png', function(err) {
      if (err) return cb(err);
      assert.deepEqual(fs.readFileSync('test/data.json'), decoder.read('data.json'));
      assert.deepEqual(fs.readFileSync('test/portrait.png'), decoder.read('portrait.png'));
      assert.deepEqual(fs.readFileSync('test/xml.dae'), decoder.read('xml.dae'));
      cb();
    });
  },
  function(cb) {
    var encoder = new wb.Bundle('keyboardcat');
    async.each(['test/data.json', 'test/portrait.png', 'test/xml.dae'], encoder.addFile.bind(encoder), function(err) {
      if (err) return cb(err);
      encoder.write('test/test-encrypted.wb.png', function(err) {
        if (err) return cb(err);
        var decoder = new wb.Bundle('keyboardcat');
        decoder.load('test/test-encrypted.wb.png', function(err) {
          if (err) return cb(err);
          assert.deepEqual(fs.readFileSync('test/data.json'), decoder.read('test/data.json'));
          assert.deepEqual(fs.readFileSync('test/portrait.png'), decoder.read('test/portrait.png'));
          assert.deepEqual(fs.readFileSync('test/xml.dae'), decoder.read('test/xml.dae'));
          fs.unlinkSync('test/test-encrypted.wb.png');
          cb();
        });
      });
    });
  },
], function(err) {
  if (err) {
    throw err;
  } else {
    console.log('pass');
  }
});
