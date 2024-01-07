


## GPT Instructions (Prompt)

Name: �w�w�|�p�E v4.1.0

�A�O�w�w�|�p�E�������A�D�n�����Ȧ�:
1. ��U�ȤH�D��ӫ~�A�Y���u�f�ӫ~�h���ˡC��U�ȤH�[�J�ʪ����������b
2. ��U�ȤH���U�|���άO�i�樭���{�ҡA�d�߷|����T�P�q�����
3. ���ȤH���ʫ�ĳ, �άO�w�����B�P�w���������ĳ
4. �N���ȤH�A�I�s���b API ��������æ��߭q��

�`�N�ƶ�:
1. ���ʤ��e�ȭ� API �^�����ӫ~��T
2. �p�G�A�L�k�B�z�A�άO���z�� API ���ϥΤ覡�A�п�X�U�C��T�A�ЫȤH�����s����:
- �ʪ������e
- �|����T
- API Request (�p�G������)
- API Response (�p�G������)
3. �Y�Ȥ�߰ݹw��d�򤺯���ʪ��ƶq, �ө��`���U���u�f����, API �ä��|���T����, �u���պ� (estimate) �ʪ������B�ɤ~�ા�D�C�ΥH�U���{�Ǩӭp��:
- �ιw�Ⱓ�H����A�w���i�ʶR���ƶq�A�åB��s�ʪ������e�A�i��պ�
- �ˬd���b���B�P�w��O�_�����R��h���ӫ~? �����ܭp�⵲�b���B�P�w�⪺�t�B�A���W����P�_�ٯ�A�h�R�X��ӫ~�A�ç�s�ʪ�����A�պ⵲�b���B�C���Ƴo�B�J����L�k�A�W�[����
- �^���ʪ������e���Ȥ�T�{�ɡA���s�I�s�@�� API �պ⵲�G�����
4. �n�D�Ȥᴣ�Ѩ������Ҹ�T�A�ô��Ѥ�I�N�X (PaymentId)�C�T�{�P�N��i�浲�b
5. ���� API �ݭn�q�L�����{�Ҥ~��ϥΡC�и߰ݫȤH���n��T���o access token, �ëO�s�Ω���� API �I�s�C��h�p�U:
- �Y�ȤH�w�O�|���A�߰ݱb���W�� (name) �H�αK�X (password) �N���ȤH�n�J���o token
- �Y�ȤH�٤��O�|���A�߰ݱb���W�� (name) �N���ȤH���U�|�����o token
- �O�d�n�J�άO���U���o�� access token, �åΩ���� API �I�s, ���� access token ���ġA�άO�ȤH���T������A�ݭn��U

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
    }
  ],

  "paths": { ... }
}
```



## ��ͬ���

- 2024/01/07 20:00, [�w�w�|�p�E v4 - �Ĥ@����ͬ���](https://chat.openai.com/share/b07bb31b-ce44-4f9f-9063-ff309c5a6ef7)
- 2024/01/08 02:27, [�w�w�|�p�E v4.1.0 - �ĤG����ͬ���](https://chat.openai.com/share/47d2bfaa-ad39-4086-8a35-6059fee4130a)

```text
2024-01-07T18:10:30.303175095Z: [INFO]  Hosting environment: Production
2024-01-07T18:10:30.304298374Z: [INFO]  Content root path: /app
2024-01-07T18:10:30.311616390Z: [INFO]  Now listening on: http://[::]:8081
2024-01-07T18:10:34.084185555Z: [INFO]     _____
2024-01-07T18:10:34.084221658Z: [INFO]    /  _  \ __________ _________   ____
2024-01-07T18:10:34.084226758Z: [INFO]   /  /_\  \\___   /  |  \_  __ \_/ __ \
2024-01-07T18:10:34.084230858Z: [INFO]  /    |    \/    /|  |  /|  | \/\  ___/
2024-01-07T18:10:34.084234658Z: [INFO]  \____|__  /_____ \____/ |__|    \___  >
2024-01-07T18:10:34.084238959Z: [INFO]          \/      \/                  \/
2024-01-07T18:10:34.084242759Z: [INFO]  A P P   S E R V I C E   O N   L I N U X
2024-01-07T18:10:34.084246459Z: [INFO]
2024-01-07T18:10:34.084250060Z: [INFO]  Documentation: http://aka.ms/webapp-linux
2024-01-07T18:10:34.084253760Z: [INFO]  Dotnet quickstart: https://aka.ms/dotnet-qs
2024-01-07T18:10:34.084257360Z: [INFO]  ASP .NETCore Version: 8.0.0
2024-01-07T18:10:34.084582483Z: [INFO]  Note: Any data outside '/home' is not persisted
2024-01-07T18:10:37.818029753Z: [INFO]  Starting OpenBSD Secure Shell server: sshd.
2024-01-07T18:10:38.211414896Z: [INFO]  Starting periodic command scheduler: cron.
2024-01-07T18:10:38.328362624Z: [INFO]  Running oryx create-script -appPath /home/site/wwwroot -output /opt/startup/startup.sh -defaultAppFilePath /defaulthome/hostingstart/hostingstart.dll     -bindPort 8080 -bindPort2 '' -userStartupCommand 'dotnet AndrewDemo.NetConf2023.API.dll'
2024-01-07T18:10:38.614125584Z: [INFO]  Could not find build manifest file at '/home/site/wwwroot/oryx-manifest.toml'
2024-01-07T18:10:38.614160787Z: [INFO]  Could not find operation ID in manifest. Generating an operation id...
2024-01-07T18:10:38.637230090Z: [INFO]  Build Operation ID: 279bd94c-a90b-4002-b60f-9ba624b6eef5
2024-01-07T18:10:40.640530998Z: [INFO]
2024-01-07T18:10:40.640567900Z: [INFO]  Agent extension
2024-01-07T18:10:40.640575301Z: [INFO]  Before if loop >> DotNet Runtime
2024-01-07T18:10:40.741579553Z: [INFO]  DotNet Runtime 8.0Writing output script to '/opt/startup/startup.sh'
2024-01-07T18:10:40.850431054Z: [INFO]  Running user provided startup command...
2024-01-07T18:10:45.832161960Z: [INFO]  info: Microsoft.Hosting.Lifetime[14]
2024-01-07T18:10:45.832209963Z: [INFO]        Now listening on: http://[::]:8080
2024-01-07T18:10:45.883388734Z: [INFO]  info: Microsoft.Hosting.Lifetime[0]
2024-01-07T18:10:45.883415036Z: [INFO]        Application started. Press Ctrl+C to shut down.
2024-01-07T18:10:45.883421136Z: [INFO]  info: Microsoft.Hosting.Lifetime[0]
2024-01-07T18:10:45.883426137Z: [INFO]        Hosting environment: Production
2024-01-07T18:10:45.883430637Z: [INFO]  info: Microsoft.Hosting.Lifetime[0]
2024-01-07T18:10:45.883435137Z: [INFO]        Content root path: /home/site/wwwroot
2024-01-07T18:12:30  No new trace in the past 1 min(s).
2024-01-07T18:13:30  No new trace in the past 2 min(s).
2024-01-07T18:14:30  No new trace in the past 3 min(s).
2024-01-07T18:15:30  No new trace in the past 4 min(s).
2024-01-07T18:16:15.670785606Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/member/login.
2024-01-07T18:16:16.028210677Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/member/login.
2024-01-07T18:16:48.989564323Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/member/register.
2024-01-07T18:16:56.777975857Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/member/e1f411b95b3149c9abdc35db3cbb4927/orders.
2024-01-07T18:17:32.742469001Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/products.
2024-01-07T18:17:44.002162485Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/create.
2024-01-07T18:17:48.353605277Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/items.
2024-01-07T18:17:51.476112146Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/estimate.
2024-01-07T18:17:51.487664399Z: [INFO]  - [1] 18��(���: $65) x 15,     $975
2024-01-07T18:17:51.505279354Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:17:51.506448851Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:17:51.507424531Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:17:51.507440533Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:17:51.514848844Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:17:51.514872046Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:17:51.514878547Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:18:08.369297298Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/items.
2024-01-07T18:18:13.002355286Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/estimate.
2024-01-07T18:18:13.008844013Z: [INFO]  - [1] 18��(���: $65) x 16,     $1040
2024-01-07T18:18:13.009556471Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:18:13.010100715Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:18:13.010594755Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:18:13.016971673Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:18:13.017287898Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:18:13.017561921Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:18:13.017795139Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:18:13.018020058Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:19:30  No new trace in the past 1 min(s).
2024-01-07T18:19:57.646871230Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/items.
2024-01-07T18:20:00.884260293Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/estimate.
2024-01-07T18:20:00.884312096Z: [INFO]  - [1] 18��(���: $65) x 17,     $1105
2024-01-07T18:20:00.893921318Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:00.893947920Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:00.893954621Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:00.893959821Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:00.893981423Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:00.893986423Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:00.893991223Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:00.893997324Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:05.883045524Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/items.
2024-01-07T18:20:18.632108373Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/estimate.
2024-01-07T18:20:18.632147776Z: [INFO]  - [1] 18��(���: $65) x 18,     $1170
2024-01-07T18:20:18.632154776Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:18.632159776Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:18.632164277Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:18.632168777Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:18.632173577Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:18.632178078Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:18.632182478Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:18.632186778Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:18.632191179Z: [INFO]  - [�u�f] 18�� �ĤG�󤻧��u�f,   $-26.0
2024-01-07T18:20:53.793254153Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/checkout/create.
2024-01-07T18:22:09.331076940Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/checkout/create.
2024-01-07T18:22:41.218364417Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/checkout/complete.
2024-01-07T18:22:57.546838511Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/checkout/complete.
2024-01-07T18:22:57.569334746Z: [INFO]  [waiting-room] issue ticket: 1 @ 01/07/2024 18:22:57 (estimate: 01/07/2024 18:22:59)
2024-01-07T18:22:57.570304716Z: [INFO]  [checkout] check system status, please wait ...
2024-01-07T18:22:59.578779829Z: [INFO]  [checkout] checkout process start...
2024-01-07T18:22:59.589819431Z: [INFO]  [checkout] checkout process complete... order created(1)
2024-01-07T18:22:59.598919992Z: [INFO]
2024-01-07T18:24:30  No new trace in the past 1 min(s).
2024-01-07T18:25:30  No new trace in the past 2 min(s).
2024-01-07T18:26:30  No new trace in the past 3 min(s).
2024-01-07T18:27:30  No new trace in the past 4 min(s).
```

- 2024/01/08 02:36, [�w�w�|�p�E v4.1.0 - �ĤT����ͬ���](https://chat.openai.com/share/8abc03ac-28c1-46ec-b928-4e76391a1af0), �|�D�ʫ�ĳ�ڶW�X�@�I�w�⪺��ĳ�F (�w�� 1000, ��ĳ 1001 ���ӫ~)