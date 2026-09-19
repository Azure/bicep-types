# Bicep Types

## Resource Test.DiscriminatedUnions/DiscriminatedUnionTest@2025-08-15
* **Readable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup, Extension
* **Writable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup, Extension
### Properties
* **customDiscriminatorAndEnvelope**: [CustomDicriminatorAndEnvelopeProperty](#customdicriminatorandenvelopeproperty) (Required)
* **customDiscriminatorAndEnvelopeWithExplicitEnvelope**: [CustomDicriminatorAndEnvelopePropertyWithExplicitEnvelope](#customdicriminatorandenvelopepropertywithexplicitenvelope) (Required)
* **default**: [DefaultDiscriminatedUnion](#defaultdiscriminatedunion) (Required)
* **inline**: [InlineDiscriminator](#inlinediscriminator) (Required)

## CustomCat
### Properties
* **meow**: 'yes' (Required)
* **name**: string (Required)

## CustomCat
### Properties
* **meow**: 'yes' (Required)
* **name**: string (Required)

## CustomDicriminatorAndEnvelopeProperty
* **Discriminator**: petKind

### Base Properties
* **petKind**: 'Cat' | 'Dog' (Required)

### envelope
#### Properties
* **pet**: [CustomCat](#customcat) (Required)
* **petKind**: 'Cat' (Required)

### envelope
#### Properties
* **pet**: [CustomDog](#customdog) (Required)
* **petKind**: 'Dog' (Required)


## CustomDicriminatorAndEnvelopePropertyWithExplicitEnvelope
* **Discriminator**: petKind

### Base Properties
* **petKind**: 'Cat' | 'Dog' (Required)

### envelope
#### Properties
* **pet**: [CustomCat](#customcat) (Required)
* **petKind**: 'Cat' (Required)

### envelope
#### Properties
* **pet**: [CustomDog](#customdog) (Required)
* **petKind**: 'Dog' (Required)


## CustomDog
### Properties
* **bark**: 'yes' (Required)
* **name**: string (Required)

## CustomDog
### Properties
* **bark**: 'yes' (Required)
* **name**: string (Required)

## DefaultCat
### Properties
* **meow**: 'yes' (Required)
* **name**: string (Required)

## DefaultDiscriminatedUnion
* **Discriminator**: kind

### Base Properties
* **kind**: 'Cat' | 'Dog' (Required)

### envelope
#### Properties
* **kind**: 'Cat' (Required)
* **value**: [DefaultCat](#defaultcat) (Required)

### envelope
#### Properties
* **kind**: 'Dog' (Required)
* **value**: [DefaultDog](#defaultdog) (Required)


## DefaultDog
### Properties
* **bark**: 'yes' (Required)
* **name**: string (Required)

## InlineDiscriminator
* **Discriminator**: petKind

### Base Properties
* **petKind**: 'Cat' | 'Dog' (Required)

### InlineCat
#### Properties
* **meow**: 'yes' (Required)
* **name**: string (Required)
* **petKind**: 'Cat' (Required)

### InlineDog
#### Properties
* **bark**: 'yes' (Required)
* **name**: string (Required)
* **petKind**: 'Dog' (Required)


