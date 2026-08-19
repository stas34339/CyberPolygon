using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace CyberPolygon.Services
{
    public class VsphereApiClient
    {
        private readonly HttpClient _httpClient;

        // Вставь сюда IP своего ESXi и логин/пароль
        private readonly string _esxiIp = "172.16.31.106";
        private readonly string _username = "root";
        private readonly string _password = "MaDCadP@$S23";

        public string VCenterIp => _esxiIp;

        public VsphereApiClient()
        {
            // Игнорируем самоподписанные сертификаты ESXi
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _httpClient = new HttpClient(handler);
        }

        public async Task<string> GetConsoleTicketAsync(string vmName)
        {
            // 1. Авторизуемся и получаем cookie сессии
            var loginSoap = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:vim25"">
   <soapenv:Body>
      <urn:Login>
         <urn:_this type=""SessionManager"">ha-sessionmgr</urn:_this>
         <urn:userName>{_username}</urn:userName>
         <urn:password>{_password}</urn:password>
      </urn:Login>
   </soapenv:Body>
</soapenv:Envelope>";

            var loginRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{_esxiIp}/sdk")
            {
                Content = new StringContent(loginSoap, Encoding.UTF8, "text/xml")
            };
            loginRequest.Headers.Add("SOAPAction", "\"urn:vim25/5.5\""); // Обрати внимание на добавленные внутренние кавычки

            var loginResponse = await _httpClient.SendAsync(loginRequest);
            if (!loginResponse.IsSuccessStatusCode)
            {
                // Читаем тело ответа даже при ошибке
                var errorXml = await loginResponse.Content.ReadAsStringAsync();
                throw new Exception($"Авторизация не удалась. HTTP {(int)loginResponse.StatusCode}. Ответ: {errorXml}");
            }

            // Извлекаем Cookie (vmware_soap_session)
            var cookie = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault()?.Split(';')[0];
            _httpClient.DefaultRequestHeaders.Add("Cookie", cookie);

            // 2. Ищем ВМ по имени
            // На ESXi мы можем получить все ВМ и просто найти нужную по имени
            var findVmSoap = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:vim25"">
   <soapenv:Body>
      <urn:RetrieveProperties>
         <urn:_this type=""PropertyCollector"">ha-property-collector</urn:_this>
         <urn:specSet>
            <urn:propSet>
               <urn:type>VirtualMachine</urn:type>
               <urn:pathSet>name</urn:pathSet>
            </urn:propSet>
            <urn:objectSet>
               <urn:obj type=""Folder"">ha-folder-vm</urn:obj>
               <urn:skip>false</urn:skip>
               <urn:selectSet xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""urn:TraversalSpec"">
                  <urn:name>traverseChild</urn:name>
                  <urn:type>Folder</urn:type>
                  <urn:path>childEntity</urn:path>
                  <urn:skip>false</urn:skip>
               </urn:selectSet>
            </urn:objectSet>
         </urn:specSet>
      </urn:RetrieveProperties>
   </soapenv:Body>
</soapenv:Envelope>";

            var findRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{_esxiIp}/sdk")
            {
                Content = new StringContent(findVmSoap, Encoding.UTF8, "text/xml")
            };
            findRequest.Headers.Add("SOAPAction", "urn:vim25/5.5");
            var findResponse = await _httpClient.SendAsync(findRequest);
            var findXml = await findResponse.Content.ReadAsStringAsync();

            var xdoc = XDocument.Parse(findXml);
            var ns = XNamespace.Get("urn:vim25");

            // Ищем нужную ВМ
            string? vmId = null;
            foreach (var propSet in xdoc.Descendants(ns + "propSet"))
            {
                var nameVal = propSet.Element(ns + "val")?.Value;
                if (nameVal == vmName)
                {
                    vmId = propSet.Parent?.Element(ns + "obj")?.Value;
                    break;
                }
            }

            if (string.IsNullOrEmpty(vmId))
            {
                throw new Exception($"Виртуальная машина '{vmName}' не найдена.");
            }

            // 3. Получаем тикет WebMKS
            var ticketSoap = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:vim25"">
   <soapenv:Body>
      <urn:AcquireTicket>
         <urn:_this type=""VirtualMachine"">{vmId}</urn:_this>
         <urn:ticketType>webmks</urn:ticketType>
      </urn:AcquireTicket>
   </soapenv:Body>
</soapenv:Envelope>";

            var ticketRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{_esxiIp}/sdk")
            {
                Content = new StringContent(ticketSoap, Encoding.UTF8, "text/xml")
            };
            ticketRequest.Headers.Add("SOAPAction", "urn:vim25/5.5");
            var ticketResponse = await _httpClient.SendAsync(ticketRequest);
            var ticketXml = await ticketResponse.Content.ReadAsStringAsync();

            var ticketDoc = XDocument.Parse(ticketXml);
            var ticketVal = ticketDoc.Descendants(ns + "ticket").FirstOrDefault()?.Value;
            var hostVal = ticketDoc.Descendants(ns + "host").FirstOrDefault()?.Value;
            var portVal = ticketDoc.Descendants(ns + "port").FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(ticketVal))
            {
                throw new Exception("Не удалось получить тикет WebMKS. Возможно машина выключена.");
            }
            // ЕСЛИ ESXI НЕ ВЕРНУЛ IP-АДРЕС, БЕРЕМ ТОТ, К КОТОРОМУ ПОДКЛЮЧАЛИСЬ
            if (string.IsNullOrEmpty(hostVal))
            {
                hostVal = _esxiIp;
            }

            // Если порт тоже не пришел, стандартный для WebMKS - 443
            if (string.IsNullOrEmpty(portVal))
            {
                portVal = "443";
            }

            // Завершаем сессию, чтобы не забивать память на ESXi
            var logoutSoap = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:vim25""><soapenv:Body><urn:Logout><urn:_this type=""SessionManager"">ha-sessionmgr</urn:_this></urn:Logout></soapenv:Body></soapenv:Envelope>";
            var logoutRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{_esxiIp}/sdk") { Content = new StringContent(logoutSoap, Encoding.UTF8, "text/xml") };
            logoutRequest.Headers.Add("SOAPAction", "urn:vim25/5.5");
            await _httpClient.SendAsync(logoutRequest);

            // Собираем и возвращаем URL
            return $"wss://{hostVal}:{portVal}/ticket/{ticketVal}";
        }
    }
}