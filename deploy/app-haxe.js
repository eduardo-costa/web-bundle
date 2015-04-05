(function (console) { "use strict";
var Main = function() { };
Main.main = function() {
	console.log("WebBundle> Haxe Example");
	var b = new Bundle();
	console.log(b);
};
Main.main();
})(typeof console != "undefined" ? console : {log:function(){}});
