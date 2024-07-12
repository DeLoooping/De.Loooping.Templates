# De.Loooping.Templates

This is a slim templating library implemented in .NET 8. It provides a simple way to generate text-based output by replacing placeholders in a template file or string with actual values.

## :warning: SECURITY WARNING :warning:

Templates can execute arbitrary code. There is no check for malicious content inside the templates.  
DO NOT ALLOW UNTRUSTED TEMPLATES TO BE EXECUTED IN YOUR ENVIRONMENT!

## Features
- Easy syntax, inspired by Jinja2
- Full support of C# code inside the template
- Built-in value formatting via [.NET format strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/formatting-types)
- Error tracking, so exceptions return the error location inside your template
- Type safety for parameters passed into the template

## Dependencies:
- Target frameworks .NET 7.0 and .NET 8.0 are supported.
- For compatibility reasons, only C# 11 features are currently supported.

## Installation
- Add NuGet package `De.Loooping.Templates.Core` to your project.
- If you want to use the configuration extensions, also add `De.Loooping.Templates.Configuration`.

## Basic template syntax

A template consists of different elements that are used in turns to produce the intended output.  

These elements are:


### Content blocks
Syntax: `{{ here.comes.an.expression }}`

Content blocks begin with the opening delimiter `{{` and end with the closing delimiter `}}`. Between these two delimiters must be a [C# expression](https://learn.microsoft.com/de-de/dotnet/csharp/language-reference/language-specification/expressions), i.e. a value or some code that represents a value.

Examples:
- Strings: `"abc"`
- Numbers: `42` or `12.34`
- Complex objects: `new DateTime()`
- Code that results in some value: `DateTime.Parse("2024-03-07 21:25")`

Values in content blocks can be formatted by adding a `:` after the expression, followed by a [format string](https://learn.microsoft.com/en-us/dotnet/standard/base-types/formatting-types).

Examples:
- `{{ 12.34 :000.000}}` results in the output `012.340`
- `{{ DateTime.Parse("2024-03-07"):dd.MM.yyyy}}` results in `07.03.2024`

Expression and format string can stretch over several lines, but the format string must not end with any whitespace.


### Statement blocks
Syntax: `{% here.comes.a.statement(); or.maybe = more.than.one; %}`

Statement blocks can contain any C# code that is allowed inside a method body. You can use it for example to create loops.

Example:
`{%  for(int i=0; i<=3; i++) {  %}{{ i }}{%  }  %}` results in `0123`  
The first statement block contains the for loop and the opening `{`.  
The content block in the middle `{{ i }}` just outputs the current value of `i`.
The statement block at the end finishes the loop with a closing `}`.

In case you need to output some values from inside a statement block, this is also possible by using `yield return`, but the returned value must be a string.

Example:
```C#
{%
yield return "a";
yield return "b";
%}
```  
results in `ab`.

As you can see in the last example, it's also allowed to stretch statement placeholders over multiple lines.


### Comment blocks
Syntax: `{# this is a comment #}`

The content of comment blocks is ignored in the output, so you can use them to add descriptions or explanations to your template.


### Custom blocks
Syntax: `{$CUSTOM_BLOCK_IDENTIFIER:content string$}`

Custom blocks have a unique identifier and process a `content` string to provide some output. The syntax of the `content` string and
the output created are defined by the custom blocks implementation.


### Literals

Literals have no special Syntax or delimiters. Everything that is not a content, statement, comment or custom block is a literal.  
Literals will be added to the output as-is, so usually the biggest part of a template consists of literals.

Example:
- `Left literal -> {{ 21*2 }} <- right literal` results in `Left literal -> 42 <- right literal`


## Working with templates in your code

Building and using a template from a string is as easy as this:
```C#
public void Main()
{
  string templateString = "{{ 2*21 }}";
  TemplateBuilder templateBuilder = new TemplateBuilder(templateString);
  var template = templateBuilder.Build();
  var output = template();
  Console.WriteLine(output); // will write "42" to the console
}
```

Most of the time you will want to pass some data to your template. For this, you have to define a delegate with result type `string` and use it as a type argument when creating the `TemplateBuilder`:
```C#
private delegate string MyTemplate(int a, string b);

public void Main()
{
  string templateString = "{{ a }} horses go to the {{ b }}";
  TemplateBuilder templateBuilder = new TemplateBuilder<MyTemplate>(templateString);
  var template = templateBuilder.Build();
  var output = template(42, "river");
  Console.WriteLine(output); // will write "42 horses go to the river" to the console
}
```

## Building and using custom blocks

Custom blocks must implement the interface `ICustomBlock`.

An example of a simple custom block is the predefined `EnvironmentVariableContentBlock`. It takes the name of a environment variable and returns its content:
```
public class EnvironmentVariableContentBlock: ICustomBlock
{
    public string DefaultIdentifier => "ENV";
    
    public string Evaluate(string content)
    {
        return Environment.GetEnvironmentVariable(content) ?? String.Empty;
    }
}
```

Usage:
```
public void Main()
{
  string templateString = "{$ENV:VariableName$}";
  TemplateBuilder templateBuilder = new TemplateBuilder(templateString);
  templateBuilder.AddCustomBlock(new EnvironmentVariableContentBlock());
  var template = templateBuilder.Build();
  var output = template();
  Console.WriteLine(output); // will write the content of environment variable 'VariableName' to the console
}
```

If you want to use an identifier that is different to the custom blocks default identifier, you can do so:
```
  templateBuilder.AddCustomBlock(new EnvironmentVariableContentBlock(), "ANOTHER_IDENTIFIER");
```

HINT: custom block identifiers must not contain any whitespaces!


## Changing delimiters

In seldom cases you might need to output special character sequences that interfere with the templating engines default delimiters (i.e. `{{`, `}}`, `{%`, `%}`, `{#`, `#}`).

One solution to this is to return your needed output from a content or statement placeholder:
- `{{ "{{ lalala }}" }}` will output `{{ lalala }}`
- `{% yield return "{% lalala %}"; %}` will output `{% lalala %}`

but this looks nasty and is very inconvenient.

Another solution is to overwrite the default delimiters with something you don't need to output from your template:
```C#
public void Main()
{
  string templateString = "{{ 2*21 }}";
  TemplateBuilder templateBuilder = new TemplateBuilder(templateString, new TemplateProcessorConfiguration()
  {
      LeftContentDelimiter = "{{{",
      RightContentDelimiter = "}}}"
  });
  var template = templateBuilder.Build();
  var output = template();
  Console.WriteLine(output); // will write "{{ 2*21 }}" to the console
}
```

### Adding custom types

The TemplateBuilder will only add references and [`using` directives](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-directive) for the most basic types like `int` and `string`, as well as references of all types provided in the delegate type argument (`TDelegate` in `new TemplateBuilder<TDelegate>()`). If you want to use or declare other types (like a generic `List<T>` for example), you have to add them to the `TemplateBuilder` before building the template or it will throw an exception:
```C#
public void Main()
{
  string templateString = "{{ DateTime.Parse("2024-03-07"):dd.MM.yyyy}}";
  TemplateBuilder templateBuilder = new TemplateBuilder(templateString)
    .WithType<DateTime>();
  var template = templateBuilder.Build();
  var output = template();
  Console.WriteLine(output); // will write "07.03.2024" to the console
}
```

`TemplateBuilder.WithType<T>()` adds the required assembly reference to the template and also adds the types namespace as a `using` directive.
You can also use `TemplateBuilder.WithReference(Assembly reference)` and/or `TemplateBuilder.WithUsing(String @using)` to add these separately.


### Configuration extensions

The configuration extensions allow you to add templating to your JSON configurations. To use these, you have to add NuGet package `De.Loooping.Templates.Configuration.XXX.nupkg`.

By default, block delimiters are embedded into block comments (i.e. /* and */) inside the JSON document, so that colissions with Intellisense are kept to a minimum.

Example appsettings.template.json:
```
{
  "AnInteger": /*{{ 21*2 }}*/,
  "AListOfStrings": [/*{%
    for(int i=0; i<3; i++)
    {
      if (i != 0) yield return ",";
      yield return $"\"item_{i}\"";
    }
  %}*/]
}
```
will evaluate as
```
{
  "AnInteger": 42,
  "AListOfStrings": ["item_0","item_1","item_2"]
}
```

Add the template file to dependency injection:
```C#
public void Configure(ConfigurationManager configuration)
{
  configuration.AddJsonTemplateFile("appsettings.template.json");
}
```

If you need to add types, references or usings to the `TemplateBuilder` or change any of the delimiters:
```C#
public void Configure(ConfigurationManager configuration)
{
  configuration.AddJsonTemplateFile("appsettings.template.json",
    build: builder =>
    {
        builder.AddType(typeof(List<>));
        builder.AddReference(typeof(IDictionary<,>).Assembly);
        builder.AddUsing("Custom.Namespace");
        builder.Configuration.LeftContentDelimiter = "{{{";
    });
}
```

If you need to inject any data into the template:
```C#
private delegate string MyTemplate(string myString);
...
public void Configure(ConfigurationManager configuration)
{
  configuration.AddJsonTemplateFile<MyTemplate>("appsettings.template.json",
    (inject) => inject("This will be injected as variable myString into the template")
  );
}
```