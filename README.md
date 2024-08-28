# MSBuild CodeGen

A custom MSBuild task for code generators with discovered dependencies (e.g. `.d` files). The task properties
match `CustomBuild` with the addition of a new `DependencyFile` property. The dependency file format
is Makefile syntax like what gcc produces with `gcc -MD -MF $out.d`.

## Usage

Load the task from the DLL:

```
  <UsingTask TaskName="CodeGen" AssemblyFile="{dll path}" />
```

Create an `ItemGroup` for your custom file type:

```
<ItemGroup>
  <MyFile Include="{input path}">
    <Command>{command to execute}</Command>
    <Outputs>{output path}</Outputs>
    <AdditionalInputs>{tool path}</AdditionalInputs>
    <DependencyFile>{dependency path}</DependencyFile>
  </MyFile>
  <MyFile>
    ...
  </MyFile>
</ItemGroup>
```

Create a `Target` that invokes the task:

```
  <Target Name="CodeGenMyFile" BeforeTargets="ClCompile">
    <CodeGen Inputs="@(MyFile)" TargetName="CodeGenMyFile" TLogLocation="$(TLogLocation)" />
  </Target>
```

The `TargetName` property must match the name of the `Target` so that the generated `.tlog` files will
be recognized by Visual Studio.

The command must create a dependency file listing the input path and its dependencies using Makefile syntax:

```
{input path}: {dependency path1} {dependency path2} ...
```
