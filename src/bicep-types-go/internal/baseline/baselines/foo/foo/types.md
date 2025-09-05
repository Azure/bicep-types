# Bicep Types

## Resource foo@v1
* **Readable Scope(s)**: None
* **Writable Scope(s)**: None
### Properties
* **abc**: string: Abc prop
* **arrayType**: any[] {minLength: 1, maxLength: 10}: Array of any
* **def**: [def](#def) (ReadOnly): Def prop
* **dictType**: [dictType](#dicttype): Dictionary of any
* **ghi**: bool (WriteOnly): Ghi prop
* **jkl**: [jkl](#jkl) (Required, Identifier): Jkl prop

### Function doSomething
* **Output**: bool
#### Parameters
0. **arg**: string
1. **arg2**: string

## def
### Properties

## dictType
*Sensitive*
### Properties
### Additional Properties
* **Additional Properties Type**: any

## jkl
### Properties