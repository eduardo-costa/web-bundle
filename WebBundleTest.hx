package;
import js.Browser;
import js.Bundle;
import js.html.Element;
import js.html.Image;

/**
 * 
 * @author Eduardo Pons - eduardo@thelaborat.org
 */
class WebBundleTest
{

	/**
	 * Entry point.
	 */
	static public function main():Void
	{		
		trace("WebBundle> Haxe Example");
		
		//creates an empty Bundle.
		var b : Bundle = new Bundle();
		
		//Loads an example bundle from the data folder.
		b.load("data/resource-pass.wb",function(d:Bundle,p:Float) 
		{
			//check load progress.
			trace(p);
			
			//if 100%
			if(p>=1)
			{
				//if data is valid.
				if(d==null)return;			
				
				//reads an image from the 'portrait.jpg' byte buffer.
				d.readImg("portrait.png",function(img:Image)
				{
					Browser.document.body.appendChild(img);
				});			
				
				//loads a json string.
				trace(d.readText("data.json"));
				
				//loads a json parsed string.
				trace(d.readJSON("data.json"));
				
				//loads a XML string.
				trace(d.readText("xml.dae").substr(0,500));
			}		
		},"12345");	
		
	}
	
}