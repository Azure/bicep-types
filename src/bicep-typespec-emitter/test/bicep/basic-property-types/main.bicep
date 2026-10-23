extension 'test.tgz'  as ext

resource test 'Microsoft.Example/ExampleExtension@2020-10-01' = {
  BoolProperty: true
  Int64Property: 123456789
  StringArrayProperty: [
    'value1'
    'value2'
  ]
  StringBoolProperty: 'true'
  StringProperty: 'Hello there!'
  TemplatizedProperty: {
    value: 'exampleValue'
  }
}
