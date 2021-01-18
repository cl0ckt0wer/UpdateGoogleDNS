using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace UpdateGoogleDNS
{
    class Program
    {
        //based off documentation at https://support.google.com/domains/answer/6147083?hl=en#zippy=%2Cusing-the-api-to-update-your-dynamic-dns-record
        static readonly HttpClient client = new HttpClient();
        static readonly string[] UsernameFlags = new string[] { "/un", "/username" };
        static readonly string[] PasswordFlags = new string[] { "/pw", "/password" };
        static readonly string[] DomainFlags = new string[] { "/d", "/domain" };
        static readonly string[] EmailFlags = new string[] { "/e", "/email" };

        static async Task Main(string[] args)
        {
            if (args.Any(x => x.StartsWith("/?") || x.StartsWith("/help")))
            {
                Console.WriteLine("Example Usage:");
                Console.WriteLine("UpdateGoogleDns.exe /un:username /pw:password /d:www.mydomain.test /email:admin@mydomain.test");
                Console.WriteLine("UpdateGoogleDns.exe /username:username /password:password /domain:www.mydomain.test /email:admin@mydomain.test");
                return;
            }
            //parse arguments
            string username = ParseMyArg(UsernameFlags, args);
            string password = ParseMyArg(PasswordFlags, args);
            string domain = ParseMyArg(DomainFlags, args);
            //where am i supposed to put the email in the request?
            //works without it...

            //string email = ParseMyArg(EmailFlags, args);
            
            var updateip = $"https://domains.google.com/nic/update?hostname={domain}=";
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
            
                //check what ips our domain is currently registerd as
                IPHostEntry? registeredip;
                try
                {
                    //if there aren't any entries we will throw an exception
                    registeredip = await Dns.GetHostEntryAsync(domain);
                }
                catch (SocketException sx)
                {
                    Console.WriteLine(sx);
                    Console.WriteLine($"{domain} doesn't have an ip registered");
                    registeredip = new IPHostEntry();
                }
                var ipresponse = await client.GetStringAsync($"https://domains.google.com/checkip");
                Console.WriteLine($"My external address is {ipresponse}");

                IPAddress? ip;
                IPAddress.TryParse(ipresponse, out ip);
              
                //google doesn't let you do this without an agent string set

                client.DefaultRequestHeaders.UserAgent.TryParseAdd("AAAreYouReadingThisLetsGetABeerWhenAllThisIsOver/19516343803");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Basic", Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                               $"{username}:{password}")));
                
             
                //update address if it's not correct
                if (registeredip.AddressList == null || !registeredip.AddressList.Contains(ip))
                {
                    Console.WriteLine("Need to Update");
                    var updaterequest = $"{updateip}{ip}";
                    Console.WriteLine($"Update Request\n{updaterequest}");
                    var mygetstring = await client.GetStringAsync(updaterequest);
                    Console.WriteLine($"Update Response \n{mygetstring}");
                }
                else
                {
                    Console.WriteLine("DNS is correct, skipping update");
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                throw;
            }
        }
        private static string ParseMyArg(string[] ids, string[] input)
        {
            foreach (var i in ids)
            {
                foreach (var t in input)
                {
                    if (t.StartsWith(i)){
                        return t.Substring(t.IndexOf(i) + i.Length + 1);
                    }   
                }
            }
            throw new ArgumentException();
        }
    }
   

}
