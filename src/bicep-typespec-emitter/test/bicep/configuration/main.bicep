extension 'test.tgz' with {
  configProperty: true
}

resource example 'Microsoft.Example/ConfigurableExtension@2025-11-11' = {
  StringProperty: 'configurable'
}
