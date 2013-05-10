# SWFTRAWL #

Command-line program to get some data out of Flash swf files (Windows, C#).

* List of classes in a swf or list of swfs
* List of fonts or symbols
* List of classes (etc) present in all swfs except a specific swf
* List of classes (etc) from a specific swf which are also present in a range of other swfs
* Exclude XML file (useful for legacy AS2 work) generated from combinations of the above

Use 'swftrawl --help' for help.

Requires SwfOp.dll and PluginCore.dll (from FlashDevelop) to be in the same directory as swftrawl.exe.

SwfOp is licensed under the LGPL 2.1, and PluginCore has an MIT license - I think it should be fine to include them here
but if not I'll certainly remove them.

## Examples: ##

Write a file with a list of all classes in Foo.swf:

    swftrawl --swf Foo.swf --classlist foo_classes.txt

Write a file with a list of distinct classes which are all swfs but not in Foo.swf:

    dir /B /S *.swf | swftrawl --swfpipe --classlist classes.txt --omititemsfrom Bar\Foo.swf --merge

Write a file with a list of classes in each swf that are also present in Foo.swf:

    dir /B /S *.swf | swftrawl --swfpipe --classlist classes.txt --onlyitemsfrom Bar\Foo.swf

Write an exclude xml file containing all classes in Foo.swf:

    swftrawl --swf Foo.swf --exclude exclude.xml


