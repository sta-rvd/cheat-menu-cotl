#r "..\bin\cheat_menu.dll"

using cheat_menu;

if(File.Exists("../doc/cheatNames.txt")){
    File.Delete("../doc/cheatNames.txt");
}


List<Definition> cheatMethods = DefinitionManager.GetAllCheatMethods();

List<string> cheatFnNames = new List<string>();

foreach(var cheatMethod in cheatMethods){
    cheatFnNames.Add(cheatMethod.MethodInfo.Name);
}

cheatFnNames.Sort();
File.WriteAllLines("../doc/cheatNames.txt", cheatFnNames);