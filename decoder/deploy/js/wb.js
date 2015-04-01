/**
Creates the Bundle manager.
//*/
var Bundle = function Bundle(p_store_buffer)
{
	var ref = this;
	/**
	Container of the loaded bundles.
	//*/
	ref.data = {};
	
	/**
	Origin URL if this bundle was loaded.
	//*/
	ref.url = "";
	
	/**
	Byte buffer with all data.
	//*/
	ref.buffer = null;
	
	/**
	Flag that Indicates if the full loaded buffer will be stored.
	//*/	
	ref.storeBuffer = p_store_buffer==null ? false : p_store_buffer;
	
	/**
	Shortcut to the rendering context  2d.
	//*/
	ref.g = null;
		
	/**
	Reference to the header entries with file information.
	//*/
	ref.entries = [];
	
	/**
	Parses a buffer with getImageData into a pixel buffer.
	//*/
	ref.parsePixels =
	function(p_buffer,p_callback,p_type)
	{			
		//Creates and cache the context.
		ref.g = (ref.g==null) ?  document.createElement("canvas").getContext("2d") : ref.g;
		ref.g.imageSmoothingEnabled = false;
		return ref.parseImg(p_buffer,function(p_img)
		{
			var cw = p_img.width;
			var ch = p_img.height;
			ref.g.canvas.width 	= cw;
			ref.g.canvas.height = ch;			
			ref.g.drawImage(p_img, 0, 0);			
			var data = ref.g.getImageData(0, 0, cw, ch);			
			var d = data.data;							
			var cc = d.length / (cw*ch);
			if(p_callback != null) p_callback(d,cw,ch,cc);
		},p_type);
	};
	
	/**
	Parses a <img> tag using as input the buffer with bytes of an image file.
	//*/
	ref.parseImg = 
	function(p_buffer,p_callback,p_type)
	{
		var mt   = p_type==null ? "application/octet-stream" : p_type;
		var blob = new Blob( [ p_buffer ],{ type: mt });
		var URL  = window.URL || window.webkitURL;			
		var img  = new Image();
		img.onload = 
		function(ev)
		{			
			if(p_callback!=null)
			{				
				p_callback(img);			
			}
		};
		img.onerror = 
		function(ev)
		{			
			console.error("bundle> failed to parse <img>");
		};
		img.src = URL.createObjectURL(blob);
		return img;
	};
	
	/**
	Parses an ArrayBuffer of Bundle data.
	//*/
	ref.parse = function(p_buffer,p_callback)
	{
		var img = 
		ref.parsePixels(p_buffer,function(p_pixels,p_w,p_h,p_channels)
		{
			var k  = 0;	
			var h  = "";			
			var pc = p_w * p_h;
			var bb = 			
			new Uint8Array(pc * 3);
			
			if(ref.storeBuffer) ref.buffer = bb;
			
			//ignores alpha channel. canvas do alpha premultiplication that spoils the RGB values so it must always be 1.0.
			for(var i=0;i<p_pixels.length;i++)
			{
				if(((i+1)%4)==0) continue;				
				bb[k++] = p_pixels[i];
			}
			
			k=0;
			
			//extract entries header string
			while(k < bb.length)
			{
				if(bb[k]<5)   { k++; break; } //was testing 0 but possibly the encoding messed up the value
				h += String.fromCharCode(bb[k++]);
			}
			
			//init entries list
			ref.entries = [];			
			
			//entries are separated by ';' and also '\n' must be ignored
			var htks = h.split("\n");
			h = htks.join("");
			htks = h.split(";");
			
			//create header data structures [path,type,byte_count]
			for(var i=0;i<htks.length;i++)
			{
				if(htks[i]=="") continue;
				var hdtks = htks[i].split(",");
				var hd    = {};
				hd.path   = hdtks[0];
				hd.type   = hdtks[1];
				hd.length = parseInt(hdtks[2]);
				ref.entries.push(hd);
			}
			
			//for each entry extract the Uint8Array sector from the bundle buffer.
			var el = ref.entries;		
			//var ss="";
			//console.log("parsing "+el.length+" entries");
			for(var i=0;i<el.length;i++)
			{
				var hd    = el[i];				
				var eb    = bb.subarray(k,k+hd.length);				
				k+=hd.length;
				//ss="";
				//console.log("==== "+hd.path+" ["+eb.length+"]");
				//for(var j=0;j<10;j++) ss+="["+j+","+eb[j]+",'"+String.fromCharCode(eb[j])+"']\n";
				//ss+="...\n";
				//for(var j=0;j<10;j++) { var m = (eb.length-10)+j; ss+="["+m+","+eb[m]+",'"+String.fromCharCode(eb[m])+"']\n"; }
				//console.log(ss);
				ref.data[hd.path] = eb;
				//console.log("====");
			}						
			
			/*
			var txt = ref.readText("1x1.png.txt").split(" ");
			var bin = ref.read("1x1.png");
			ss = "";
			for(var i=0;i<bin.length;i++)
			{
				var s0 = txt[i];
				var s1 = bin[i]+"";
				if(s0 != s1) console.log(i+" "+s0+" "+s1);
				//ss += "["+txt[i]+","+bin[i]+"]\n";
			}
			//*/
			if(p_callback != null) p_callback(ref);			
		});			
	};
	
	/**
	Loads a bundle file and parses its content storing all ArrayBuffers into the correct places.
	//*/
	ref.load =
	function(p_url,p_callback)
	{
		ref.url = p_url;
		var ld = new XMLHttpRequest();		
		ld.open( "GET", p_url, true );		
		ld.responseType = "arraybuffer";
		//if (ld.overrideMimeType != null) {  ld.overrideMimeType(p_binary ? "application/octet-stream" : "text/plain");  }		
		ld.onprogress 	= function(p_event)
		{			
			var bytesLoaded = p_event.loaded;
			var bytesTotal  = p_event.total;
			var progress = (p_event.total<=0 ? 0 : (p_event.loaded / (p_event.total+5))) * 0.999;
			if(p_callback != null) p_callback(null,progress);
		};		
		ld.onload = function(p_load_event) 
		{	
			ref.parse(ld.response,function(p_bundle)
			{
				if(p_callback != null) p_callback(p_bundle,1.0);
			});
		};
		ld.send();
	};
	
	/**
	Indicates if a given data buffer exists in this bundle.
	//*/
	ref.exists = function(p_id) { return ref.data[p_id] != null; }
	
	/**
	Returns the ArrayBuffer of a given bundled data.
	//*/
	ref.read =
	function(p_id) { if(ref.data[p_id]==null){ console.error("bundle> "+p_id+" not found."); } return ref.data[p_id]; };
	
	/**
	Returns the specified data as string.
	//*/
	ref.readText = 
	function(p_id) 
	{
		var d = ref.read(p_id);				
		var s = "";
		if(d==null) return s;
		for(var i=0;i<d.length;i++) s+= String.fromCharCode(d[i]);
		return s;		
	};
	
	/**
	Returns the specified data as a JSON Object.
	//*/
	ref.readJSON = 
	function(p_id) 
	{ 
		var s = ref.readText(p_id);
		return s=="" ? {} : JSON.parse(s);
	};
	
	/**
	Reads the specified data as a HTMLElement.
	//*/
	ref.readElement = 
	function(p_id,p_element)
	{ 		
		var tag = document.createElement(p_element);
		tag.textContent = ref.readText(p_id);
		return tag;
	};
	
	/**
	Reads the specified data as a script tag.
	//*/
	ref.readScript = function(p_id) { return ref.readElement(p_id,"script"); }
	
	/**
	Reads the specified data as a css tag.
	//*/
	ref.readCSS = function(p_id) { return ref.readElement(p_id,"style"); }
	
	/**
	Reads an <img> tag from the buffer.
	//*/
	ref.readImg =
	function(p_id,p_callback) { return ref.parseImg(ref.read(p_id),p_callback); };
	
	/**
	Reads the specified data as a image pixel buffer.
	//*/
	ref.readRGBA = function(p_id,p_callback,p_type) { ref.parsePixels(ref.read(p_id),p_callback,p_type); };
	
	
};



