var async = require('async'),
  assert = require('assert'),
  fs = require('fs'),
  wb = require('../tool');

async.series([
  function (cb) {
    var decoder = new wb.Decoder();
    decoder.load('test/data.wb.png', function(err) {
      if (err) return cb(err);
      assert.deepEqual(fs.readFileSync('test/data.json'), decoder.read('data.json'));
      assert.deepEqual(fs.readFileSync('test/portrait.png'), decoder.read('portrait.png'));
      assert.deepEqual(fs.readFileSync('test/xml.dae'), decoder.read('xml.dae'));
      cb();
    });
  }
], function(err) {
  if (err) {
    throw err;
  } else {
    console.log('pass');
  }
});
