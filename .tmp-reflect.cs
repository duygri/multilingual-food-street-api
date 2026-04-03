using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
var path = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.nuget\packages\xamarin.googleandroid.libraries.places\5.1.1.2\lib\net9.0-android35.0\Xamarin.GoogleAndroid.Libraries.Places.dll");
try {
  var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
  foreach (var t in asm.GetTypes().Where(t => t.FullName != null && (t.FullName.Contains("Places") || t.FullName.Contains("Place") || t.FullName.Contains("Autocomplete"))).OrderBy(t => t.FullName).Take(120)) {
    Console.WriteLine(t.FullName);
  }
}
catch (Exception ex) { Console.WriteLine(ex); }
