(function (console) { "use strict";
var HxOverrides = function() { };
HxOverrides.substr = function(s,pos,len) {
	if(pos != null && pos != 0 && len != null && len < 0) return "";
	if(len == null) len = s.length;
	if(pos < 0) {
		pos = s.length + pos;
		if(pos < 0) pos = 0;
	} else if(len < 0) len = s.length + len - pos;
	return s.substr(pos,len);
};
var Main = function() { };
Main.main = function() {
	console.log("WebBundle> Haxe Example");
	var b = new Bundle();
	b.load("data/resource-pass.wb",function(d,p) {
		console.log(p);
		if(p >= 1) {
			if(d == null) return;
			d.readImg("portrait.png",function(img) {
				window.document.body.appendChild(img);
			});
			console.log(d.readText("data.json"));
			console.log(d.readJSON("data.json"));
			console.log((function($this) {
				var $r;
				var _this = d.readText("xml.dae");
				$r = HxOverrides.substr(_this,0,500);
				return $r;
			}(this)));
		}
	},"12345");
};
Main.main();
})(typeof console != "undefined" ? console : {log:function(){}});
