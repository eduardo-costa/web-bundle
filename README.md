# Web Bundle

* Tool to pack binary files into a PNG image.
* Users can load a `bundle.wp` file and extract its data indexed by the original file's path.
  * A file in the folder `root/img/logo.png` can be accessed by `bundle.read("img/logo.png") == Uint8Array`
  * Helper methods allow users to read the desired data format.
    * `var d = bundle.readJSON("data/file.json") == object`
	* `var s = bundle.readText("data/file.txt") == string`
	* `var img = bundle.readImg("img/logo.jpg") == ImageElement`

## Example Bundle
This PNG contains the following files:  

* xml.dae
* data.json
* portrait.png  

![packed bundle](https://dl.dropboxusercontent.com/u/20655747/resource.png)

# Why Bundle stuff ?

* Significantly reduce the number of HTTP requests allowing fast page loads
* Text data can be at least 40% compressed
* Browsers decompression routines are native and fast
* Games can greatly benefit of the compression and data packing
* No need to create and manage your own pack data type

## Compression
* Find the `wb.exe` tool in `tool\bin\Release`
* In the command line call `wb.exe -v -i root_folder -o output.wb`
* Data can be encrypted using `-h some-password`
* After execution the compressed data will be stored in the `output.wb` file.
* Move this file to your page desired folder.

## Decompression [Javascript]
* Add `deploy/js/wb.js` or `deploy/js/wb.min.js`  to your page. 
```
var b = new Bundle();
b.load("data/output.wb",function(bundle,progress)
{
	if(progress >= 1.0)
	{
		if(bundle != null)
		{
			bundle.read[Text|JSON|Img|...]("data-id");
		}
	}
},[password]);
```

## Decompression [Haxe]
* Make sure `<script src='js/wb.js'/>` exists in your page.
* Make sure the `web-bundle` library is installed and linked in your project.
```
import js.Bundle;

var b : Bundle = new Bundle();
b.load("data/output.wb",function(bundle:Bundle,progress:Float)
{
	if(progress >= 1.0)
	{
		if(bundle != null)
		{
			bundle.read[Text|JSON|Img|...]("data-id");
		}
	}
},[password]);
```

## Decompression [CLI]
* Find the `wb.exe` tool in `tool\bin\Release`
* In the command line call `wb.exe -d -v -i output.wb -o target_folder`
* If data is encrypted use `-h some-password`
* After decompression the data will be available at `target_folder/`

## TODO
* Use `nodejs` for the tool and make it platform independent and/or a webservice.

