# Bicep Types

## Resource Test.DegenerateUnions/DegenerateUnion@2025-08-15
* **Readable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup, Extension
* **Writable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup, Extension
### Properties
* **customDiscriminatorAndEnvelope**: [envelope](#envelope) (Required)
* **customDiscriminatorAndEnvelopeWithExplicitEnvelope**: [envelope](#envelope) (Required)
* **default**: [envelope](#envelope) (Required)
* **inline**: [InlineVariant](#inlinevariant) (Required)

## CustomVariant
### Properties
* **name**: string (Required)
* **test**: 'hello there' (Required)

## CustomVariant
### Properties
* **name**: string (Required)
* **test**: 'hello there' (Required)

## DefaultVariant
### Properties
* **name**: string (Required)
* **test**: 'hello' (Required)

## envelope
### Properties
* **custom**: [CustomVariant](#customvariant) (Required)
* **customKind**: 'One' (Required)

## envelope
### Properties
* **custom**: [CustomVariant](#customvariant) (Required)
* **customKind**: 'One' (Required)

## envelope
### Properties
* **kind**: 'One' (Required)
* **value**: [DefaultVariant](#defaultvariant) (Required)

## InlineVariant
### Properties
* **customKind**: 'One' (Required)
* **name**: string (Required)
* **test**: 'hello world' (Required)

