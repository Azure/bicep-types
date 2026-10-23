extension 'test.tgz'

resource version1 'Microsoft.Example/exampleExtension@2025-07-01' = {
  One: 'one'
  Zero: 'zero'
}

resource version2 'Microsoft.Example/exampleExtension@2025-08-01' = {
  One: 'one'
  Two: 'two'
  Zero: 'zero'
}

resource version3 'Microsoft.Example/exampleExtension@2025-09-01' = {
  Three: 'three'
  Two: 'two'
  Zero: 'zero'
}
