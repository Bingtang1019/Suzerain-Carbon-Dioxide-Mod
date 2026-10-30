using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SuzerainTrainer
{
    
    
    
    
    
    
    
    
    
    
    internal interface IModPage
    {
        
        string GroupTitle { get; }

        
        string PageTitle { get; }

        
        Control Build();

        
        bool Installed();
    }

    
    
    
    
    internal static class ModPageRegistry
    {
        static readonly List<IModPage> pages = new List<IModPage>();

        
        internal static void Register(IModPage page)
        {
            if (page == null || pages.Contains(page)) return;
            pages.Add(page);
        }

        
        internal static List<IModPage> AllPages { get { return new List<IModPage>(pages); } }

        
        
        
        
        internal static List<IModPage> InstalledPages()
        {
            List<IModPage> list = new List<IModPage>();
            foreach (IModPage p in pages)
            {
                if (p == null) continue;
                try { if (p.Installed()) list.Add(p); }
                catch { }
            }
            return list;
        }
    }
}
