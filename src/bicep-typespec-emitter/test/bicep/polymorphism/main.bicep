extension 'test.tgz'  as ext

resource catVariants 'Test.Polymorphism/PolymorphismTest@2025-08-15' = {
  default: {
    name: 'Fluffy'
    meow: 'yes'
    petKind: 'cat'
  }
}

resource dogVariants 'Test.Polymorphism/PolymorphismTest@2025-08-15' = {
  default: {
    name: 'Rex'
    bark: 'yes'
    petKind: 'dog'
  }
}
