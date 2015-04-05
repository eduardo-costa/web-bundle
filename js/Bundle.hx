package js;
import js.html.Element;
import js.html.Image;
import js.html.ImageElement;
import js.html.ScriptElement;
import js.html.StyleElement;
import js.html.Uint8Array;


/**
 * Class that describes a data from the WebBundle.
 */
extern class BundleEntry
{

	/**
	 * Path to the data.
	 */
	public var path : String;
	
	/**
	 * Extension of the bundle data.
	 */
	public var type : String;
	
	/**
	 * Length in bytes of the data.
	 */
	public var length : Int;
	
}

/**
 * Class that represents a WebBundle instance.
 * @author Eduardo Pons - eduardo@thelaborat.org
 */
@:native("Bundle")
extern class Bundle
{
	
	/**
	 * Flag that indicates if the WebBundle raw buffer will be stored.
	 */
	public var storeBuffer : Bool;

	/**
	 * Buffer with all data
	 */
	public var buffer : Uint8Array;
	
	/**
	 * Object that contains all stored bytes decoded from the parsed WebBundle.
	 */
	public var data : Dynamic;

	/**
	 * Origin URL from where this bundle was loaded.
	 */
	public var url :String;
	
	/**
	 * List of data entries.
	 */
	public var entries : Array<BundleEntry>;
	
	/**
	 * Creates an empty Bundle.
	 */
	@:overload(function():Void{})
	public function new(p_store_buffer:Bool);	
	
	/**
	 * Loads a WebBundle from web and fill the instance with all data.
	 */
	@:overload(function(p_url:String, p_callback : Bundle-> Float->Void):Void{})
	public function load(p_url:String, p_callback : Bundle-> Float->Void, p_pass:String):Void;
	
	/**
	 * Parses the byte buffer and returns a Bundle instance.
	 */
	@:overload(function(p_buffer : Uint8Array, p_callback : Bundle-> Float->Void):Void{})
	public function parse(p_buffer : Uint8Array, p_callback : Bundle-> Void, p_pass:String):Void;
	
	/**
	 * Tells if a given data entry exists in this bundle.
	 * @param	p_id
	 * @return
	 */
	public function exists(p_id:String):Bool;
	
	/**
	 * Reads a data from this bundle in a given path.
	 * @param	p_id
	 * @return
	 */
	public function read(p_id:String):Uint8Array;
	
	/**
	 * Reads a data from this bundle and converts it to `string`
	 * @param	p_id
	 * @return
	 */
	public function readText(p_id:String):String;
	
	/**
	 * Reads a data from this bundle and converts it to a JSON `object`
	 * @param	p_id
	 * @return
	 */	
	public function readJSON(p_id:String):Dynamic;
	
	/**
	 * Reads a data from this bundle as string and fills the `textContent`of the desired HTMLElement.
	 * @param	p_id
	 * @return
	 */	
	public function readElement(p_id:String, p_element:String):Element;
	
	/**
	 * Reads a data from this bundle and creates a new `Image` to be added to the DOM.
	 * @param	p_id
	 * @return
	 */
	@:overload(function(p_id:String):Image{})
	public function readImg(p_id:String,p_callback:Image->Void):Image;
	
	/**
	 * Reads a data from this bundle and returns it as a `<script>`.
	 * @param	p_id
	 * @return
	 */
	public function readScript(p_id:String):ScriptElement;
	
	/**
	 * Reads a data from this bundle and returns it as a `<style>`.
	 * @param	p_id
	 * @return
	 */
	public function readCSS(p_id:String):StyleElement;
	
	/**
	 * Reads a data from this bundle and converts it to a `DataURL` encoded file.
	 * @param	p_id
	 * @return
	 */
	public function readDataURL(p_id:String):String;
	
	/**
	 * Reads image data from this bundle and returns the raw pixel buffer in the format [RGBARGBARG...]
	 * @param	p_id
	 * @param	p_callback
	 * @return
	 */
	public function readRGBA(p_id:String,p_callback : Uint8Array->Void):Void;
	
}