extension 'test.tgz'

#disable-next-line BCP081
resource generic1 'Microsoft.DoesNotExist/fake@2025-01-01' = {
  prop1: 'hello'
  prop2: 42
}

#disable-next-line BCP081
resource generic2 'Microsoft.DoesNotExist/fake@2025-01-01' = {
  prop1: 'there'
  prop2: 31
}
