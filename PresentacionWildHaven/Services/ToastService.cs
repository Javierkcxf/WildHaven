using System;
using System.Threading.Tasks;

namespace PresentacionWildHaven.Services
{
    public class ToastService
    {
        public event Func<string, string, Task>? OnShow;

        public async Task MostrarAsync(string mensaje, string tipo = "info")
        {
            if (OnShow != null)
                await OnShow.Invoke(mensaje, tipo);
        }
    }
}
