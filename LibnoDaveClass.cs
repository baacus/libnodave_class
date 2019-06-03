using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class LibnoDaveClass
{
    public enum PLCDataType
    {
        DInteger,
        Integer,
        Byte
    }
    public enum AddressType
    {
        Input,
        Output,
        Memory,
        DB
    }
    private libnodave.daveOSserialType fds;
    private libnodave.daveInterface di;
    private libnodave.daveConnection dc;
    private string ip; private int port, slot, rack;
    private AddressType all1RType; private int all1DbNo, all1ByteNo, all1BitNo; private bool isallControl;
    private bool isConnected, isDisconnected;
    public static byte[] bitSetle = new byte[1] { 1 };
    public static byte[] bitResetle = new byte[1] { 0 };
    public bool IsConnected { get { return isConnected; } }
    public bool IsDisconnected { get { return isDisconnected; } }

    /// <summary>
    /// <para>Siemens S7-1200 ile haberlesen kutuphanedir.</para>
    /// <para>Haberlesmenin mumkun olmasi icin PLC programinda PUT/GET haberlesmeye izin verilmesi gereklidir.</para>
    /// <para> Input(I), Output(O), Memory(M), DataBlock(DB) adreslerini okumak/yazmak mumkundur.
    /// Okunacak/yazilacak adres tipini belirlemek icin AddressType tipi kullanilmalidir.
    /// </para>
    /// </summary>
    /// <param name="libnoPort"> Belirtilmedigi surece 102 dir. </param>
    /// <param name="libnoIP"> PLC nin ip adresidir. </param>
    /// <param name="libnoRack"> Belirtilmedigi surece 0 dir. </param>
    /// <param name="libnoSlot"> Belirtilmedigi surece 1 dir. </param>
    /// <param name="isAlwaysControl"> Haberlesme dogrulugunu artirmak icin always TRUE olan bitin kontrolunu belirtir. "True" ise always TRUE kontrolu yapilir.</param>
    /// <param name="alwaysTrueAType"> PLC nin always TRUE olan bitinin adres tipidir (Memory veya DB olabilir, Input veya Output olamaz!). isAlwaysControl=false tanimlandiysa deger girmeye gerek yoktur. </param>
    /// <param name="alwaysTrueDbNo"> PLC nin always TRUE olan bitinin DB numarasidir. isAlwaysControl=false tanimlandiysa deger girmeye gerek yoktur. </param>
    /// <param name="alwaysTrueByteNo"> PLC nin always TRUE olan bitinin byte numarasidir. isAlwaysControl=false tanimlandiysa deger girmeye gerek yoktur. </param>
    /// <param name="alwaysTrueBitNo"> PLC nin always TRUE olan bitinin bit numarasidir. isAlwaysControl=false tanimlandiysa deger girmeye gerek yoktur. </param>
    public LibnoDaveClass(int libnoPort, string libnoIP, int libnoRack, int libnoSlot, bool isAlwaysControl, AddressType alwaysTrueAType = AddressType.Memory, int alwaysTrueDbNo = 0, int alwaysTrueByteNo = 0, int alwaysTrueBitNo = 0)
    {
        port = libnoPort;
        ip = libnoIP;
        rack = libnoRack;
        slot = libnoSlot;
        isallControl = isAlwaysControl;
        all1RType = alwaysTrueAType;
        all1DbNo = alwaysTrueDbNo;
        all1ByteNo = alwaysTrueByteNo;
        all1BitNo = alwaysTrueBitNo;
    }


    /// <summary>
    /// PLC ye baglanmak icin kullanilan fonksiyondur. isConnected özelliginden baglanti durumu kontrol edilebilir.
    ///Bu fonksiyon hata durumunda false degeri doner.
    /// </summary>
    /// <returns></returns>
    public bool libno_Dave_Baglan()
    {
        bool sonuc = false;
        try
        {
            fds.rfd = libnodave.openSocket(port, ip);
            fds.wfd = fds.rfd;
            di = new libnodave.daveInterface(fds, "FD1", 0, libnodave.daveProtoISOTCP, libnodave.daveSpeed187k);
            di.setTimeout(10000);
            dc = new libnodave.daveConnection(di, 0, rack, slot);
            if (fds.rfd > 0 && dc.connectPLC() == 0)
            {
                sonuc = true;
                isDisconnected = false;
            }
            else
                sonuc = false;
        }
        catch (Exception e)
        {
            hata_gonder(e, "LIBNO BAGLANTI KURMA");
            sonuc = false;
        }
        isConnected = sonuc;
        return sonuc;
    }

    /// <summary>
    /// PLC baglantisini kesmek icin kullanilan fonksiyondur. isDisconnected özelliginden baglanti durumu kontrol edilebilir.
    /// Bu fonksiyon hata durumunda false degeri doner.
    /// </summary>
    /// <returns></returns>
    public bool libno_Dave_BaglantiKopar()
    {
        bool sonuc = false;
        if (fds.rfd > 0)
        {
            try
            {
                di.disconnectAdapter();
                dc.disconnectPLC();
                libnodave.closePort(fds.wfd);
                sonuc = true;
                isConnected = false;
            }
            catch (Exception e)
            {
                sonuc = false;
                hata_gonder(e, "LIBNO BAGLANTI KOPARMA");
            }
        }
        isDisconnected = sonuc;
        return sonuc;
    }

    private bool libno_Always_Check()
    {
        bool sonuc = false;
        byte[] okunanByte = new byte[1];
        int okunanDeger = -1;
        if (isallControl)
        {
            if (all1RType == AddressType.Input || all1RType == AddressType.Output) return sonuc;

            int daveInt = (all1RType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
            int daveDB = (all1RType == AddressType.Memory) ? 0 : all1DbNo;
            try
            {
                okunanDeger = dc.readBits(daveInt, daveDB, all1ByteNo * 8 + all1BitNo, 1, okunanByte);
                if (okunanDeger == 0)
                {
                    if (okunanByte[0] == 1) sonuc = true;
                    else sonuc = false;
                }
                else return sonuc;

            }
            catch (Exception e)
            {
            }
        }
        else sonuc = true;

        return sonuc;

    }

    /// <summary>
    /// Memory tipi adres okunurken DB no girilmesine gerek yoktur, default olarak 0 girilebilir.
    /// Adres tipi Memory veya DB olabilir, Input veya Output olamaz!
    /// Bu fonksiyon hata durumunda false degeri doner.
    /// </summary>
    /// <param name="aType">Yazilacak adresin tipi belirlenir. AddressType kullanilir.</param>
    /// <param name="dbNo">Yazilacak adresin DB numarasidir.</param>
    /// <param name="byteNo">Yazilacak adresin baslangic byte numarasidir.</param>
    /// <param name="byteList">Yazilacak byte degerleridir. Sirasiyla bytelarin alacagi degerleri iceren string elemanli List icermelidir.
    /// "10010101" seklinde bir eleman varsa o byte in bitleri sirasiyla bu degerlere esitlenir. 
    /// </param>
    /// <returns></returns>
    public bool libno_Dave_ByteYaz(AddressType aType, int dbNo, int byteNo, List<string> byteList)
    {
        bool sonuc = false;
        byte[] okunanByte = new byte[byteList.Count];
        byte[] byteDeger = new byte[byteList.Count];

        for (int i = 0; i < byteList.Count; i++)
        {
            string writeByte = reverse_string(byteList[i]);
            byte outByte = 0;
            try
            {
                outByte = Convert.ToByte(writeByte, 2);
            }
            catch
            {
                return sonuc;
            }
            byteDeger[i] = outByte;
        }
        int denemeSayaci = 0;
        int sonucDeger = -1;
        int sonucDeger2 = -1;
        if (all1RType == AddressType.Input || all1RType == AddressType.Output) return sonuc;

        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
        int daveDB = (aType == AddressType.Memory) ? 0 : dbNo;

        try
        {
            sonucDeger = dc.readBytes(daveInt, daveDB, byteNo, okunanByte.Length, okunanByte);
            bool always1Check = libno_Always_Check();
            if (sonucDeger != 0 || !always1Check)
                return sonuc;
        }
        catch (Exception e)
        {
            hata_gonder(e, "LIBNO BYTE YAZMA");
            return sonuc;
        }
        if (!okunanByte.SequenceEqual(byteDeger))
        {
            while (denemeSayaci < 4)
            {
                try
                {
                    sonucDeger = dc.writeBytes(daveInt, daveDB, byteNo, okunanByte.Length, byteDeger);
                    sonucDeger2 = dc.readBytes(daveInt, daveDB, byteNo, okunanByte.Length, okunanByte);
                    if (okunanByte.SequenceEqual(byteDeger) && sonucDeger == 0 && sonucDeger2 == 0)
                    {
                        sonuc = true;
                        return sonuc;
                    }
                    else denemeSayaci += 1;
                }
                catch (Exception e)
                {
                    hata_gonder(e, "LIBNO BYTE YAZMA");
                    return sonuc;
                }
            }
        }
        else sonuc = true;
        return sonuc;
    }

    /// <summary>
    /// Memory tipi adres okunurken DB no girilmesine gerek yoktur, default olarak 0 girilebilir.
    /// Adres tipi Memory veya DB olabilir, Input veya Output olamaz!
    /// Bu fonksiyon hata durumunda false degeri doner.
    /// </summary>
    /// <param name="aType">Okunacak adresin tipi belirlenir. AddressType kullanilir.</param>
    /// <param name="dbNo">Okunacak adresin DB numarasidir.</param>
    /// <param name="byteNo">Okunacak adresin baslangic byte numarasidir.</param>
    /// <param name="length">Okunacak byte sayisidir.</param>
    /// <param name="sonucByte">Okunacak byte degeridir. Bu degiskene string elemanli List girilmelidir. 
    /// Her eleman sirasiyla okunan bytelari temsil eder. Her elemanin her char i sirasiyla okunan bitleri temsil eder. 
    /// Ornek olarak okunan bytelarin 0. byte 2. biti icin listenin 0. elemaninda 2. karaktere bakmak gerekir.</param>
    /// <returns></returns>
    public bool libno_Dave_ByteOku(AddressType aType, int dbNo, int byteNo, int length, out List<string> sonucByte)
    {
        bool sonuc = false;
        int sonucDeger = -1;
        sonucByte = new List<string>();
        List<string> byteList = new List<string>();
        byte[] okunanDeger = new byte[length];

        if (all1RType == AddressType.Input || all1RType == AddressType.Output) return sonuc;

        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
        int daveDB = (aType == AddressType.Memory) ? 0 : dbNo;
        try
        {
            sonucDeger = dc.readBytes(daveInt, daveDB, byteNo, length, okunanDeger);
            bool always1Check = libno_Always_Check();
            if (sonucDeger == 0 && always1Check)
            {
                foreach (byte okunanByte in okunanDeger)
                {
                    string okunanBitler = Convert.ToString(okunanByte, 2);
                    string readByte = "00000000";
                    System.Text.StringBuilder readByteNew = new StringBuilder(readByte);
                    for (int i = 0; i < okunanBitler.Length; i++)
                    {
                        readByteNew[i] = okunanBitler.Substring(okunanBitler.Length - 1 - i, 1).ToCharArray()[0];
                    }
                    readByte = readByteNew.ToString();
                    byteList.Add(readByte);
                }

                sonuc = true;
                sonucByte = byteList.ToList();
            }

        }
        catch (Exception e) { hata_gonder(e, "LIBNO BYTE OKUMA"); }
        return sonuc;
    }

    /// <summary>
    /// Memory tipi adres okunurken DB no girilmesine gerek yoktur, default olarak 0 girilebilir.
    /// Adres tipi Memory veya DB olabilir, Input veya Output olamaz!
    /// Bu fonksiyon hata durumunda false degeri doner.
    /// </summary>
    /// <param name="aType">Okunacak adresin tipi belirlenir. AddressType kullanilir.</param>
    /// <param name="dbNo">Okunacak adresin DB numarasidir.</param>
    /// <param name="byteNo">Okunacak adresin baslangic byte numarasidir.</param>
    /// <param name="length">Okunacak int degisken sayisidir.</param>
    /// <param name="sonucSayi">Okunacak int degeridir. Bu degiskene double elemanli List girilmelidir.
    /// Her eleman sirasiyla okunan int degerleri temsil eder.
    /// </param>
    /// <param name="degiskenTipi">Okunacak degerin PLCdeki int tipini belirtir. PLCDataType kullanilir. 
    /// Okunan deger PLC de Byte değerinde ise PLCDataType.Byte, Int(Integer) veya Word tipinde ise PLCDataType.Integer, DInt(Double-Integer) tipinde ise PLCDataType.DInteger
    /// kullanilmalidir.
    /// </param>
    /// <returns></returns>
    public bool libno_Dave_BytetoInt(AddressType aType, int dbNo, int byteNo, int length, out List<double> sonucSayi, PLCDataType degiskenTipi = PLCDataType.DInteger)
    {
        bool sonuc = false;
        int sonucDeger = -1;
        sonucSayi = new List<double>();
        int byteSayisi = degiskenTipi == PLCDataType.DInteger ? 4 : (degiskenTipi == PLCDataType.Integer ? 2 : 1);
        int byteLength = length * byteSayisi;
        byte[] okunanDeger = new byte[byteLength];

        if (all1RType == AddressType.Input || all1RType == AddressType.Output) return sonuc;

        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
        int daveDB = (aType == AddressType.Memory) ? 0 : dbNo;
        List<double> okunanSayilar = new List<double>();
        try
        {
            sonucDeger = dc.readBytes(daveInt, daveDB, byteNo, byteLength, okunanDeger);
            bool always1Check = libno_Always_Check();
            if (sonucDeger == 0 && always1Check)
            {
                double okunanSayi = 0;
                for (int i = 0; i < okunanDeger.Length; i++)
                {
                    okunanSayi += okunanDeger[i] * Math.Pow(2, 8 * ((okunanDeger.Length - i - 1) % byteSayisi));
                    if (i % byteSayisi == byteSayisi - 1)
                    {
                        okunanSayilar.Add(okunanSayi);
                        okunanSayi = 0;
                    }
                }
                sonuc = true;
                sonucSayi = okunanSayilar.ToList();
            }
        }
        catch (Exception e) { hata_gonder(e, "LIBNO INT OKUMA"); }
        return sonuc;
    }


    /// <summary>
    /// Input, Output, Memory tipi adres okunurken DB no girilmesine gerek yoktur, default olarak 0 girilebilir.
    /// Bu fonksiyon hata durumunda false degeri doner.
    /// </summary>
    /// <param name="aType">Yazilacak adresin tipi belirlenir. AddressType kullanilir.</param>
    /// <param name="dbNo">Yazilacak adresin DB numarasidir.</param>
    /// <param name="byteNo">Yazilacak adresin byte numarasidir.</param>
    /// <param name="bitNo">Yazilacak adresin bit numarasidir.</param>
    /// <param name="bitDeger">Adrese yazilacak degerdir. Biti setlemek icin True,
    ///  biti resetlemek icin False girilmelidir.
    /// </param>
    /// <param name="feedbackMode">Yazilan adresin geri okunarak dogrulanmasi istenmiyorsa false girilmelidir.</param>
    /// <returns></returns>
    public bool libno_Dave_BitYaz(AddressType aType, int dbNo, int byteNo, int bitNo, bool bitDeger, bool feedbackMode = true)
    {
        bool sonuc = false;
        byte[] byteDeger = bitDeger ? bitSetle : bitResetle;
        byte[] okunanByte = new byte[byteDeger.Length];
        int denemeSayaci = 0;
        int okunanDeger = -1;
        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : (aType == AddressType.Input ? libnodave.daveInputs : (aType == AddressType.Output ? libnodave.daveOutputs : libnodave.daveDB));
        int daveDB = (aType == AddressType.Memory || aType == AddressType.Input || aType == AddressType.Output) ? 0 : dbNo;
        try
        {
            okunanDeger = dc.readBits(daveInt, daveDB, byteNo * 8 + bitNo, 1, okunanByte);
            bool always1Check = libno_Always_Check();
            if (okunanDeger != 0 || !always1Check)
                return sonuc;
        }
        catch (Exception e)
        {
            hata_gonder(e, "LIBNO BIT YAZMA");
            return sonuc;
        }
        if (!okunanByte.SequenceEqual(byteDeger))
        {
            while (denemeSayaci < 4)
            {
                try
                {
                    okunanDeger = dc.writeBits(daveInt, daveDB, byteNo * 8 + bitNo, 1, byteDeger);
                    if (okunanDeger == 0)
                    {
                        if (feedbackMode == false)
                        {
                            for (int i = 0; i < byteDeger.Length; i++)
                            {
                                okunanByte[i] = byteDeger[i];
                            }
                        }
                        else
                        {
                            okunanDeger = dc.readBits(daveInt, daveDB, byteNo * 8 + bitNo, 1, okunanByte);
                            if (okunanDeger != 0)
                            {
                                denemeSayaci += 1;
                                continue;
                            }
                        }

                        if (okunanByte.SequenceEqual(byteDeger))
                        {
                            sonuc = true;
                            return sonuc;
                        }
                        else denemeSayaci += 1;
                    }
                    else denemeSayaci += 1;

                }
                catch (Exception e)
                {
                    hata_gonder(e, "LIBNO BIT YAZMA");
                    return sonuc;
                }
            }
        }
        else sonuc = true;
        return sonuc;
    }

    /// <summary>
    /// Input, Output, Memory tipi adres okunurken DB no girilmesine gerek yoktur, default olarak 0 girilebilir.
    /// Bu fonksiyon hata durumunda false degeri doner.
    /// </summary>
    /// <param name="aType">Okunacak adresin tipi belirlenir. AddressType kullanilir.</param>
    /// <param name="dbNo">Okunacak adresin DB numarasidir.</param>
    /// <param name="byteNo">Okunacak adresin byte numarasidir.</param>
    /// <param name="bitNo">Okunacak adresin bit numarasidir.</param>
    /// <param name="bitDeger">Okunacak adres degeridir. Bu degiskene bool tipi eleman girilmelidir.</param>
    /// <returns></returns>
    public bool libno_Dave_BitOku(AddressType aType, int dbNo, int byteNo, int bitNo, out bool bitDeger)
    {
        bool sonuc = false;
        bitDeger = false;
        byte[] okunanByte = new byte[1];
        int okunanDeger = -1;
        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : (aType == AddressType.Input ? libnodave.daveInputs : (aType == AddressType.Output ? libnodave.daveOutputs : libnodave.daveDB));
        int daveDB = (aType == AddressType.Memory || aType == AddressType.Input || aType == AddressType.Output) ? 0 : dbNo;
        try
        {
            okunanDeger = dc.readBits(daveInt, daveDB, byteNo * 8 + bitNo, 1, okunanByte);
            bool always1Check = libno_Always_Check();
            if (okunanDeger == 0 && always1Check)
            {
                if (okunanByte[0] == 1) bitDeger = true;
                else bitDeger = false;
                sonuc = true;
            }
            else return sonuc;

        }
        catch (Exception e)
        {
            hata_gonder(e, "LIBNO BIT OKUMA");
        }
        return sonuc;
    }

    private string reverse_string(string str)
    {
        char[] resultArr = str.ToCharArray();
        Array.Reverse(resultArr);
        return new string(resultArr);
    }
    private void hata_gonder(Exception e, string hataAdresi)
    {
        string hataLog = " HATANIN GORULDUGU BLOK: " + hataAdresi
                                    + "  HATA TANIMI: " + e.GetType().ToString() + "   " + e.Message;
        log_dosyasini_olustur("libno_hata_log.txt");
        hata_log_dosyasi_yaz("libno_hata_log.txt", hataLog);
    }
    private void log_dosyasini_olustur(string dosyaAdi)
    {
        Directory.CreateDirectory("C:\\Test");
        string[] getFiles = Directory.GetFiles("C:\\Test", dosyaAdi);
        if (getFiles.Count() == 0)
        {
            File.Create("C:\\Test\\" + dosyaAdi);
        }
    }
    private void hata_log_dosyasi_yaz(string dosyaAdi, string aciklama)
    {
        try
        {
            string path = "C:\\Test\\" + dosyaAdi;
            if (File.Exists(path))
            {
                FileInfo varolanDosya = new FileInfo(path);
                if (varolanDosya.Length > 10000000) //greater than 10mb
                    File.Delete(path);
            }
            using (StreamWriter dosyaYaz = new StreamWriter(path, true))
            {
                dosyaYaz.WriteLine(aciklama + "        " + GuncelZaman());
            }
        }
        catch (Exception e) { hata_gonder(e, "HATA LOG DOSYASI YAZMA"); }
    }
    private string GuncelZaman()
    {
        string guncelZaman = "";
        DateTime zaman = new DateTime();
        zaman = DateTime.Now;

        guncelZaman = zaman.Year.ToString() + "-" + zaman.Month.ToString("D2") + "-" + zaman.Day.ToString("D2") + " " + zaman.Hour.ToString("D2") + ":" + zaman.Minute.ToString("D2") + ":" + zaman.Second.ToString("D2");

        DateTime dt1 = DateTime.Parse(guncelZaman);

        return guncelZaman;
    }

}