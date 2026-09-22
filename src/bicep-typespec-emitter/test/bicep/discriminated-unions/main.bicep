extension 'test.tgz'

resource dogVariant 'Test.DiscriminatedUnions/DiscriminatedUnionTest@2025-08-15' = {
  // this is the default serialization format for discriminated unions in TypeSpec  
  default: {
    kind: 'Dog'
    value: {
      name: 'Rex'
      bark: 'yes'
    }
  }

  // this is the default serialization format for discriminated unions in TypeSpec
  // with customized discriminator property and the envelope property
  customDiscriminatorAndEnvelope: {
    pet: {
      name: 'Rex'
      bark: 'yes'
    }
    petKind: 'Dog'
  }

  // this is the same as the previous one but with explicit envelope type on the TypeSpec side
  customDiscriminatorAndEnvelopeWithExplicitEnvelope: {
    pet: {
      name: 'Rex'
      bark: 'yes'
    }
    petKind: 'Dog'
  }

  // This is a typical inline serialization format for discriminated unions that we commonly use in Azure
  inline: {
    name: 'Rex'
    bark: 'yes'
    petKind: 'Dog'
  }
}

resource catVariant 'Test.DiscriminatedUnions/DiscriminatedUnionTest@2025-08-15' = {
  // this is the default serialization format for discriminated unions in TypeSpec  
  default: {
    kind: 'Cat'
    value: {
      name: 'Fluffy'
      meow: 'yes'
    }
  }

  // this is the default serialization format for discriminated unions in TypeSpec
  // with customized discriminator property and the envelope property
  customDiscriminatorAndEnvelope: {
    pet: {
      name: 'Fluffy'
      meow: 'yes'
    }
    petKind: 'Cat'
  }

  // this is the same as the previous one but with explicit envelope type on the TypeSpec side
  customDiscriminatorAndEnvelopeWithExplicitEnvelope: {
    pet: {
      name: 'Fluffy'
      meow: 'yes'
    }
    petKind: 'Cat'
  }

  // This is a typical inline serialization format for discriminated unions that we commonly use in Azure
  inline: {
    name: 'Fluffy'
    meow: 'yes'
    petKind: 'Cat'
  }
}
