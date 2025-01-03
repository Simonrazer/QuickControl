# QuickControl
A horrible idea - get access to variables of your C# project in a Browser

This project uses NewtonsoftJson to serialize your data (including private members), host a website that displays the data, which via the magic of reflection and websocket can change all values, no matter where they are hiding.

Well there are limits to magic, or rather my will to implement. You can only change primitive values, (strings, ints..), but can not change references. This should be possible though.
It should also be possible to expose Functions and Methods and launch them from the browser. But that is a Feature for another day as well.
I haven't tested what happens if you have circular references. I read somewhere that you can configure NewtonsoftJson to do whatever you want it to then.

## Usage
1. Put HttpServer and index.html.cs in your Project
2. call `QuickControl.BrowserDisplayServer` using your root object and an adress+port
3. start it using either `Start()` or `StartThread`, depending on if you want to have it make a sepperate thread on its own
Also so Example Program

# If you use this in production you will be fired
Especially if the port you set this to host on is publically open to the internet. Anyone will be able to play arround with your privates.


## Acknowledgements
This project uses several things I copied from the internet, majorly the JSONViewer by Roman Makudera, a simple HttpServer by Benjamin N. Summerton.