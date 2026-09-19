# Bicep Types

## Resource Test.Polymorphism/PolymorphismTest@2025-08-15
* **Readable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup, Extension
* **Writable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup, Extension
### Properties
* **default**: [Pet](#pet) (Required)

## Pet
* **Discriminator**: petKind

### Base Properties
* **name**: string (Required)

### Cat
#### Properties
* **meow**: 'yes' (Required)
* **name**: string (Required)
* **petKind**: 'cat' (Required)

### Dog
#### Properties
* **bark**: 'yes' (Required)
* **name**: string (Required)
* **petKind**: 'dog' (Required)


