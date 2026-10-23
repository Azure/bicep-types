# Bicep Types

## Resource Test.Polymorphism/OneVariant@2025-08-15
* **Readable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup, Extension
* **Writable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup, Extension
### Properties
* **default**: [Pet](#pet) (Required)

## Pet
* **Discriminator**: petKind

### Base Properties
* **name**: string (Required)

### Dog
#### Properties
* **bark**: 'yes' (Required)
* **name**: string (Required)
* **petKind**: 'dog' (Required)


