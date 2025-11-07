var exT=typeof(System.Exception);
string strStart= 
    """
    public enum Exceptions
    {
        None = 0,
        Exception = 1,
        inserHere
    }
    """;
const string exName = nameof(Exception);
const string replaceLabel="inserHere";

var exceptionsTypes = exT.Assembly.GetTypes().Where(t=>t.IsAssignableTo(exT)&&t!=exT).ToArray();

exceptionsTypes=SortInheritence(exceptionsTypes);


var exNames=exceptionsTypes.Select(t=>t.Name).Distinct().Select(name =>
{
    if (!name.Contains(exName))
        return name;
    if(name.StartsWith(exName))
        return name[exName.Length..];
    if(name.EndsWith(exName))
        return name[..^exName.Length];
    return name;
}).ToArray();

var insert=string.Join(",\n    ", exNames);
var fullType=strStart.Replace(replaceLabel, insert);
Console.WriteLine(fullType);

const string excExt =
    """
    public static class ExceptionExt
    {
        [Pure]
        public static Exceptions GetEnumType(this Exceptions? ex)
        => ex switch
        {
            null => Exceptions.None,
            inserHere
        };
    }
    """;
var lines=exceptionsTypes.Select(t =>
{
    return $"{t.FullName} => {GetEnumName(t)},";
});
var extInsert=string.Join("\n        ", lines);
var extFullType = excExt.Replace(replaceLabel, extInsert);
Console.WriteLine(extFullType);

var combine= $"{fullType}\n\n{extFullType}";



static string GetEnumName(Type t)
{
    var name = t.Name;
    if (!name.Contains(exName))
        name = name;
    else if(name.StartsWith(exName))
        name= name[exName.Length..];
    else if(name.EndsWith(exName))
        name= name[..^exName.Length];
    return $"Exceptions.{name}";
}


static Type[] SortInheritence(Type[] types)
{
    var dict = types.ToDictionary(t => t, t => types.Count(other => other.IsAssignableTo(t)));
    Array.Sort(types,(t1,t2)=>Comparer<int>.Default.Compare(dict[t1],dict[t2]));
    return types;
}