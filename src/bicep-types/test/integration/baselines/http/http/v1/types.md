# Bicep Types

## Resource request@v1
* **Valid Scope(s)**: Unknown
### Properties
* **body**: any (ReadOnly): The parsed request body.
* **format**: 'json' | 'raw': How to deserialize the response body.
* **method**: string: The HTTP method to submit request to the given URI.
* **statusCode**: int (ReadOnly): The status code of the HTTP request.
* **uri**: string (Required): The HTTP request URI to submit a GET request to.

