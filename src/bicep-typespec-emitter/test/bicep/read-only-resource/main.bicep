extension 'test.tgz'

resource writeOnly 'Microsoft.Example/ExampleExtension@2020-10-01' existing = {
  StringProperty: 'Hello there!'
}

output test string = writeOnly.StringProperty
