# De.Loooping.Templates

This is a slim templating library implemented in .NET 8. It provides a simple way to generate text-based output by replacing placeholders in a template file or string with actual values.


## Features
- Easy syntax, inspired by Jinja2
- Full support of C# code inside the template
- Built-in value formatting via [.NET format strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/formatting-types)


## Basic template syntax

A template consists of different elements that are used in turns to produce the intended output.  
These elements are:


### Content placeholders
Syntax: `{{ here.comes.an.expression }}`

Content placeholders begin with the opening delimiter `{{` and end with the closing delimiter `}}`. Between these two delimiters must be a [C# expression](https://learn.microsoft.com/de-de/dotnet/csharp/language-reference/language-specification/expressions), i.e. a value or some code that represents a value.

Examples:
- Strings: `"abc"`
- Numbers: `42` or `12.34`
- Complex objects: `new DateTime()`
- Code that results in some value: `DateTime.Parse("2024-03-07 21:25")`

Values in content placeholders can be formatted by adding a `:` after the expression, followed by a [format string](https://learn.microsoft.com/en-us/dotnet/standard/base-types/formatting-types).

Examples:
- `{{ 12.34 :000.000}}` results in the output `012.340`
- `{{ DateTime.Parse("2024-03-07"):dd.MM.yyyy}}` results in `07.03.2024`

Expression and format string can stretch over several lines, but the format string must not end with any whitespace.


### Statement placeholders
Syntax: `{% here.comes.a.statement(); or.maybe = more.than.one; %}

Statement placeholders can contain any C# code that is allowed inside a method body. You can use it for example to create loops.

Example:
`{%  for(int i=0; i<=3; i++) {  %}{{ i }}{%  }  %}` results in `0123`  
The first statement block contains the for loop and the opening `{`.  
The content block in the middle `{{ i }}` just outputs the current value of `i`.
The statement block at the end finishes the loop block with a closing `}`.

In case you need to output some values from inside a statement block, this is also possible by using `yield return`, but the returned value must be a string.  
Example:
```
{%
yield return "a";
yield return "b";
%}
```  
results in `ab`.

As you can see in the last example, it's also allowed to stretch statement placeholders over multiple lines.


### Comments
Syntax: `{# this is a comment #}`

The content of comments is ignored in the output, so you can use them to add descriptions or explanations to your template.


### Literals

Literals have no special Syntax or delimiters. Everything that is not a content placeholder, statement placeholder or comment is a literal.  
Literals will be added to the output as-is, so usually the biggest part of a template consists of literals.

Example:
- `Left literal -> {{ 21*2 }} <- right literal` results in `Left literal -> 42 <- right literal`


## Working with templates in your code

Building and using a template from a string is as easy as this:  
```
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
```
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

## Changing delimiters

In seldom cases you might need to output special character sequences that interfere with the templating engines default delimiters (i.e. `{{`, `}}`, `{%`, `%}`, ...).

One solution to this is to output your needed output from inside a content or statement placeholder:
- `{{ "{{ lalala }}" }}` will output `{{ lalala }}`
- `{% yield return "{% lalala %}"; %}` will output `{% lalala %}`
but this looks nasty and is very inconvenient.

Another solution for this is to overwrite the default delimiters with something you don't need to output from your template:
```
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

The TemplateBuilder will only add references and [`using` directives](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-directive) for the most basic types like `int` and `string`, as well as all types provided in the delegate type argument (`TDelegate` in `new TemplateBuilder<TDelegate>()`). If you want to use other types (like a generic `List<T>` for example), you have to add them to the `TemplateBuilder` before building the template or it will throw an exception:  
```
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


## Still to do:
- Add meaningful expception messages in case of syntax or compile-time errors