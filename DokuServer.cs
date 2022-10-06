namespace DokuServer 
{
    public static class DokuServer 
    {
        private static WebServer server = new WebServer();
        public static void Main() {
            server.run();
        }
    }
}