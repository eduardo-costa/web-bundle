package js;

/**
 * Class that represents a WebBundle instance.
 * @author Eduardo Pons - eduardo@thelaborat.org
 */
@:native("Bundle")
extern class Bundle
{

	/**
	 * Creates an empty Bundle.
	 */
	public function new();
	
	
	/**
	 * Loads a WebBundle from web and fill the instance with all data.
	 */
	@:overload(function(p_url:String, p_callback : Bundle-> Float->Void):Void{})
	public function load(p_url:String, p_callback : Bundle-> Float->Void,p_pass:String):Void;
	
}