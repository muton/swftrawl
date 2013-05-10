using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwfOp;
using System.Xml;
using System.IO;

namespace swftrawl {

	/// <summary>
	/// Get various kinds of info from multiple swf files.
	/// 
	/// v1.1   Merge mode optional, allows stuff to be returned from each supplied file distinctly.
	/// v1.2   --onlyitemsfrom and --omititemsfrom filtering.
	/// v1.21  Swf filenames present in output even if they contain no items
	/// 
    /// tim@muton.co.uk
	/// </summary>
	class Program {


		const string helpText = @"
Gets various kinds of info from multiple swf files.
Requires PluginCore.dll and SwfOp.dll from FlashDevelop.

Files can be provided with the --swf param, or by using --swfpipe and 
redirecting output from another command, e.g:
    dir /B /S *.swf | swftrawl --swfpipe --exclude my_exclude.xml

If --merge is not used, swf file paths will be present in the output on lines
prepended by the # character. Multiple 'out' params in a single invocation is
probably not useful.

Parameters:
 --help                            This message
 --swf file_1 [file_2 file_n]      swf files to read
 --swfpipe                         Get swf list from piped input
 
 --omititemsfrom swf_file        } Don't use these together,
 --onlyitemsfrom swf_file        } it would make nooo sense.

 --merge                       Output is a list of distinct items over all swfs

 --exclude exclude_xml_filename    Write an exclude XML file (implies --merge)
 --classlist class_list_filename   Write a list of classes found
 --classlistout                    Write a list of classes to standard output
 --fontlist font_list_filename     Write a list of fonts found
 --fontlistout                     Write a list of fonts to standard output
 --symbollist sym_list_filename    Write a list of symbols found
 --symbollistout                   Write a list of symbols to standard output
";
		const string P_HELP = "help";
		const string P_SWF = "swf";
		const string P_EXCLUDE = "exclude";
		const string P_PIPE = "swfpipe";
		const string P_MERGE = "merge";
		const string P_OMITITEMS = "omititemsfrom";
		const string P_ONLYITEMS = "onlyitemsfrom";
		const string P_CLASSLIST = "classlist";
		const string P_CLASSLISTOUT = "classlistout";
		const string P_FONTLIST = "fontlist";
		const string P_FONTLISTOUT = "fontlistout";
		const string P_SYMBOLLIST = "symbollist";
		const string P_SYMBOLLISTOUT = "symbollistout";

		private static bool quiet = false;

		static void Main( string[] args ) {

			var outputsPoss = new [] { P_EXCLUDE, P_CLASSLIST, P_FONTLIST, P_SYMBOLLIST };
			var stdOutputsPoss = new [] { P_CLASSLISTOUT, P_FONTLISTOUT, P_SYMBOLLISTOUT };

			var arguments = parseParams( args );

			// go quiet now if output is going to stdout
			quiet = stdOutputsPoss.Any( op => arguments.ContainsKey( op ) );

			var errors = "";
			var swfList = arguments.ContainsKey( P_SWF ) ? arguments[P_SWF] : new List<string>();
			var showHelp = arguments.ContainsKey( P_HELP );
			var doPipe = arguments.ContainsKey( P_PIPE );
			var omitList = new List<string>();
			var filterList = new List<string>();

			if ( !doPipe && swfList.Count == 0 ) { 
				errors += "No valid input swf provided.\n"; 
			}
			var outputsFound = outputsPoss.Count( op => arguments.ContainsKey( op ) && arguments[op].Count > 0 ) 
				+ stdOutputsPoss.Count( op => arguments.ContainsKey( op ) );
			if ( outputsFound == 0 ) { errors += "No valid outputs were chosen.\n"; };

			var filters = new [] { P_OMITITEMS, P_ONLYITEMS };
			if ( filters.Any( flt => arguments.ContainsKey( flt ) && arguments[flt].Count == 0 ) ) {
				errors += "Please provide paths to swfs when using 'omit' and 'only' filters.\n";
			}

			if ( showHelp || errors.Length > 0 ) {
				var norm = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Cyan;
				writeInfo( helpText );
				Console.ForegroundColor = ConsoleColor.Magenta;
				if ( !showHelp ) { writeInfo( errors ); };
				Console.ForegroundColor = norm;
				return;
			}

			if ( doPipe ) {
				string str;
				while ( ( str = Console.ReadLine() ) != null ) {
					swfList.Add( str );
				}
			}
			var merge = arguments.ContainsKey( P_MERGE ) || swfList.Count < 2;	// merge by default if only one arg - output without file path

			var pickerDict = new Dictionary<string, Func<ContentParser, List<DeclEntry>>>();
			pickerDict.Add( P_EXCLUDE, parser => parser.Classes );
			pickerDict.Add( P_CLASSLIST, parser => parser.Classes );
			pickerDict.Add( P_CLASSLISTOUT, parser => parser.Classes );
			pickerDict.Add( P_FONTLIST, parser => parser.Fonts );
			pickerDict.Add( P_FONTLISTOUT, parser => parser.Fonts);
			pickerDict.Add( P_SYMBOLLIST, parser => parser.Symbols );
			pickerDict.Add( P_SYMBOLLISTOUT, parser => parser.Symbols );

			var possibleActions = stdOutputsPoss.Concat( outputsPoss );
			var actionQueue = possibleActions.Where( action => arguments.ContainsKey( action ) );

			var excludeActions = new [] { P_EXCLUDE };
			var fileListActions = new [] { P_CLASSLIST, P_FONTLIST, P_SYMBOLLIST };
			var outListActions = new [] { P_CLASSLISTOUT, P_FONTLISTOUT, P_SYMBOLLISTOUT };

			try {
				foreach ( var action in actionQueue ) {
					var picker = pickerDict[action];

					var omitItems = new List<string>();
					if ( arguments.ContainsKey( P_OMITITEMS ) ) {
						var omitSwfList = arguments[P_OMITITEMS];
						swfList = swfList.Except( omitSwfList ).ToList();
						omitItems = getItemList( omitSwfList, picker, true );
					}
					var onlyItems = new List<string>();
					if ( arguments.ContainsKey( P_ONLYITEMS ) ) {
						onlyItems = getItemList( arguments[P_ONLYITEMS], picker, true );
					}

					if ( excludeActions.Contains( action ) ) {
						writeExcludeXml( getItemList( swfList, picker, true, omitItems, onlyItems ), arguments[action][0] );
					} else if ( fileListActions.Contains( action ) ) {
						writeList( getItemList( swfList, picker, merge, omitItems, onlyItems ), arguments[action][0] );
					} else if ( outListActions.Contains( action ) ) {
						getItemList( swfList, picker, merge, omitItems, onlyItems ).ForEach( item => Console.WriteLine( item ) );
					}
				}
			} catch ( Exception ex ) {
				writeInfo( "\nAwww duuuude, había un problema. :(\n" );
				writeInfo( ex.Message );
			}
		}

		private static IEnumerable<ContentParser> readSwfs( List<string> swfPaths ) {
			foreach ( var swfPath in swfPaths ) {

				writeInfo( "Reading {0}", swfPath );
				var parser = new ContentParser( swfPath );
				parser.Run();

				if ( parser.Errors.Count > 0 ) {
					writeInfo( String.Join( "\n", parser.Errors ) );
					throw new Exception( string.Join( "\n", parser.Errors ) );
				}
				yield return parser;
			}
		}

		private static List<string> parseAndConvert( List<string> swfList, Func<ContentParser, List<string>> converter, bool merge ) {
			List<string> outList = new List<string>();
			if ( merge ) {
				foreach ( var parser in readSwfs( swfList ) ) {
					outList.AddRange( converter( parser ).Where( val => !outList.Contains( val ) ).ToList() );
				}
				outList.Sort();
			} else {
				foreach ( var parser in readSwfs( swfList ) ) {
					outList.Add( "#" + parser.Filename );
					var found = converter( parser ).ToList();
					if ( found.Count > 0 ) {
						found.Sort();
						outList.AddRange( found );
					}
				}
			}
			return outList;
		}

		private static List<string> getItemList( List<string> swfPaths, Func<ContentParser, List<DeclEntry>> picker, bool merge, 
			List<string> omitItems = null, List<string> onlyItems = null ) {

			if ( omitItems == null ) { omitItems = new List<string>(); };

			var output = parseAndConvert( swfPaths, parser => picker( parser ).ConvertAll( val => val.Name ), merge ).Where( item => !omitItems.Contains( item ) );
			if ( onlyItems != null && onlyItems.Count > 0 ) {
				output = output.Where( item => ( onlyItems.Contains( item ) || item[0] == '#' ) );
			}
			return output.ToList();
		}

		private static void writeExcludeXml( List<string> classList, string outputPath ) {
			var doc = new XmlDocument();
			var assets = doc.CreateElement( "excludeAssets" );
			doc.AppendChild( assets );

			classList.ForEach( name => {
				var ass = doc.CreateElement( "asset" );
				var attr = doc.CreateAttribute( "name" );
				attr.Value = name;
				ass.Attributes.Append( attr );
				assets.AppendChild( ass );
			} );

			var xSettings = new XmlWriterSettings() { Indent = true, CloseOutput = true };

			using ( var xmlWriter = XmlWriter.Create( outputPath, xSettings ) ) {
				doc.WriteTo( xmlWriter );
			}

			writeInfo( "Wrote exclude XML to {0}.", outputPath );
		}

		private static void writeList( List<string> list, string outputPath ) {

			using ( TextWriter writer = new StreamWriter( outputPath, false ) ) {
				list.ForEach( ln => writer.WriteLine( ln ) );
			}
			writeInfo( "Wrote list to {0}.", outputPath );
		}

		private static Dictionary<string, List<string>> parseParams( string[] args ) {
			var output = new Dictionary<string, List<string>>();
			var curParam = "";
			for ( var i = 0; i < args.Length; i++ ) {
				var arg = args[i];
				if ( arg.Substring( 0, 2 ) == "--" ) {
					curParam = arg.Substring( 2 );
					if ( !output.ContainsKey( curParam ) ) { output.Add( curParam, new List<string>() ); };
				} else {
					output[curParam].Add( arg );
				}
			}
			return output;
		}

		private static void writeInfo( string format, params object[] args ) {
			if ( !quiet ) {
				Console.WriteLine( format, args );
			}
		}

		private static void writeInfo( string output ) {
			if ( !quiet ) {
				Console.WriteLine( output );
			}
		}

	}
}
