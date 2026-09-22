# Bicep Types

## Resource Microsoft.Example/ExampleExtension@2020-10-01
* **Readable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup, Extension
* **Writable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup, Extension
### Properties
* **BoolProperty**: bool (Required): A boolean property
* **Int64Property**: int (Required): An int64 property
* **StringArrayProperty**: string[] (Required): An array property
* **StringBoolProperty**: 'false' | 'true' (Required): A string boolean property
* **StringProperty**: string (Required): A string property
* **TemplatizedProperty**: [PayloadProperty<string>](#payloadpropertystring) (Required)

## PayloadProperty<string>
### Properties
* **type**: string
* **value**: string (Required)

