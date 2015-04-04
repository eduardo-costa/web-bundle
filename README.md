# Web Bundle

* Tool to pack binary files into a PNG image.
* Users can load a `bundle.wp` file and extract its data indexed by the original file's path.
  * A file in the folder `root/img/logo.png` can be accessed by `bundle.read("img/logo.png") == Uint8Array`
  * Helper methods helps users to read the desired data format.
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
* Find the `wb.exe` tool in `encoder\bin\Release`
* In the command line call `wb.exe -v -i root_folder -o output.wb`
* Data can be encripted using `-h some-password`
* After execution the compressed data will be stored in the `output.wb` file.
* Move this file to your page desired folder.

## Decompression
* Add `js/wb.js` to your page.
* Creates a bundle instance `var b = new Bundle();`
* Load your `wb` file `b.load("data/output.wb",function(bundle,progress){ /*...*/ });` and keep track of the progress callback. 
* When the callback `progress` reaches `1.0` and `bundle` is a valid instance you are ready to use all your data.
* Check the `read[Text|JSON|Img|...]()` methods to know which data type can be manipulated.

## TODO
* Use `nodejs` for the tool and make it platform independent and/or a webservice.
