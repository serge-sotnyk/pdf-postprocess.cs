# pdf-postprocess.cs
This utility postprocesses texts extracted from PDF (formatting with paragraph-ending symbols, hard-hyphening), 
after OCR or simple old-style formatted texts with manual entered line breaks. Project uses 
[ML.NET](https://github.com/dotnet/machinelearning) framework.

Apache license.

# Usage

Install PdfPostprocessor package:

```
dotnet add package PdfPostprocessor
```

or 

```
Install-Package PdfPostprocessor
```

C# code:

```csharp
using PdfPostprocessor;
...
var postprocessor = new Postprocessor();
var rawText = ...
var restoredText = postprocessor.RestoreText(rawText)
```

# Train your own model
## Corpus
Annotated documents can be found in folder [corpus](corpus)

Every line in annotated documents should be marked in the following way:

```
First char - line type:
* - leave as is
+ - glue with previous
```

Texts without such marks will be skipped.

## Train process
* Add your annotated texts into train corpus.
* Run [ModelCreator](ModelCreator) project.
* New model can be found in folder [Models](Models).

To use your own model, you can use overloaded constructor of Postprocessor and load model from file of stream:

```csharp
var postprocessor = new Postprocessor(pathToNewModel);
```
