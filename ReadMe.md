# Blazor With ServiceStack Static File delivery issue ReadMe
This is the specific documentation for a gist that illustrates a problem with delivering static files from virtual directories added by a PlugIn running under .Net Core 3.0.

To compile this Gist, you will need the latest Visual Studio 2019 Preview (currently V16.3.0 Preview 2.0), and you will need the .Net Core V3.0 Preview 8 SDK installed on the development machine

The Server is created as a Generic Host that hosts either a IIS Integrated InProcess web server, or a Kestrel stand-alone web server, selectable by an environment variable.
The hosted Web Server hosts the ServiceStack middleware.  Points to note regarding the delivery of static files:

1) The GenericHost setup sets the ContentRootPath to the location where the startup assembly is found.
1) Without the GUIServices PlugIn, the webserver should not deliver any static files nor allow directory listings. The URL http:///localhost:220500/ should return 404, not found.
1) With the GUIServices PlugIn, the webserver should deliver files from "arbitrary_absolute_path01/" on the virtual path /01/, and furthermore, should deliver the contents of index.html on both the URLs http:///localhost:220500/01/ and http:///localhost:220500/01/index.html . The GUIServices PlugIn should setup multiple VirtualFileSystem mappings, and delivery of static files from multiple virtual root paths, to support multiple GUIs.
1) The file PlugIn.GUIServices.Settings.Development.txt defines the collection of GUIMappings that define the cirtual path and the physical path
1) Both GUI Projects must be 'Published', usually using the 'DevelopDebugFolderProfile
1) With the GUIServices PlugIn, request to http:///localhost:220500/ should map to one of the VirtualFileSystem mappings. The GUIServices should allow a "default" attribute to be applied to a virtual root.



 