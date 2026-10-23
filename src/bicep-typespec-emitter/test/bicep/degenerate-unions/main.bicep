extension 'test.tgz'  as ext

resource test 'Test.DegenerateUnions/DegenerateUnion@2025-08-15' = {
  default: {
    kind: 'One'
    value: {
      name: 'default'
      test: 'hello'
    }
  }
  customDiscriminatorAndEnvelope: {
    custom: {
      name: 'custom'
      test: 'hello there'
    }
    customKind: 'One'
  }
  customDiscriminatorAndEnvelopeWithExplicitEnvelope: {
    custom: {
      name: 'customExplicit'
      test: 'hello there'
    }
    customKind: 'One'
  }
  inline: {
    name: 'inline'
    customKind: 'One'
    test: 'hello world'
  }
}
