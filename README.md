




## �]�w GPT Action ���B�J

1. Debug Mode, �i�J swagger UI, �I�索�W�誺 swagger.json (/swagger/v1/swagger.json)
1. �b json ���e�[�W servers ���`�I, �ó]�w��ڴ��ѪA�Ȫ� url
1. ���e�K�b MyGPTs �� Action ���e


```jsonc
{
  "openapi": "3.0.1",
  "info": { ... },

  // �o�̥[�W servers ���`�I
  "servers": [
    {
	  "url": "https://gpt-api.azurewebsites.net"
      "description": "Test"
    }
  ],

  "paths": { ... }
}
```