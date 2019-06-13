using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplicationSample
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string ip = ask_user_input("What is ip address of PLC?");
            int port, slot, rack;
            int.TryParse(ask_user_input("What is ip address of PLC? (Default 102)"), out port);
            port = port == 0 ? 102 : port;
            int.TryParse(ask_user_input("What is ip address of PLC? (Default 0)"), out rack);
            rack = rack == 0 ? 0 : rack;
            int.TryParse(ask_user_input("What is ip address of PLC? (Default 1)"), out slot);
            slot = slot == 0 ? 1 : slot;
            string logReq = ask_user_input("Do you want to save runtime errors in C:\\LibnoDaveClass? (y/n)");
            bool logRequest = logReq == "y" ? true : false;

            LibnoDaveClass libnoInstance = new LibnoDaveClass(port, ip, rack, slot, logRequest, false);

            libnoInstance.connect_plc();

            if (libnoInstance.IsConnected)
            {
                print_message("PLC is connected!");
                bool exitCondition = false;
                while (!exitCondition)
                {
                    user_cycle(libnoInstance);

                    Console.WriteLine("");
                    string exitReq = ask_user_input("Do you want to exit? (y/n)");
                    if (exitReq == "y") exitCondition = true;
                }
                libnoInstance.disconnect_plc();
                if (libnoInstance.IsDisconnected)
                    print_message("PLC is disconnected..");
            }
            else
            {
                print_message("PLC is not connected.. Check configurations and parameters. Try again later..");
            }

            print_message("Press any key to exit..");
            Console.ReadKey();
        }

        private static void user_cycle(LibnoDaveClass libno)
        {
            string optionQuestion = Environment.NewLine +
                                    "Choose one of the options below:" + Environment.NewLine +
                                    "1: Read bytes" + Environment.NewLine +
                                    "2: Write bytes" + Environment.NewLine +
                                    "3: Read bit" + Environment.NewLine +
                                    "4: Write bit" + Environment.NewLine +
                                    "5: Read integers" + Environment.NewLine +
                                    "6: Write integers" + Environment.NewLine +
                                    "Enter the number";
            string optionReq = ask_user_input(optionQuestion);

            LibnoDaveClass.AddressType address; LibnoDaveClass.PLCDataType dataType;
            int dbNo, byteNo, bitNo, length;
            List<string> readByteList, writeByteList; List<int> readIntList, writeIntList;
            bool readBit, writeBit, isSigned;
            switch (optionReq)
            {
                case "1":
                    string addressType = ask_user_input("Which address type do you want to read? (M for Memory, D for DB)");
                    string dbNumber = ask_user_input("Enter DB address of the registers. (0 for Memory address)");
                    string byteNumber = ask_user_input("Enter the starting address of the registers");
                    string lengthNumber = ask_user_input("Enter the byte length of the registers");

                    address = addressType == "M" ? LibnoDaveClass.AddressType.Memory : LibnoDaveClass.AddressType.DB;
                    int.TryParse(dbNumber, out dbNo);
                    int.TryParse(byteNumber, out byteNo);
                    int.TryParse(lengthNumber, out length);

                    if(libno.read_byte_values(address, dbNo, byteNo, length, out readByteList))
                    {
                        print_message("Process is successful!");
                        print_message("Bytes that were read are:");
                        for(int i = 0; i < readByteList.Count; i++)
                        {
                            Console.WriteLine("Byte-" + i + " is " + readByteList[i]);
                        }
                    }
                    else
                    {
                        print_message("Some errors occured. Try to connect to PLC again..");
                    }
                    break;

                case "2":
                    string addressType2 = ask_user_input("Which address type do you want to write? (M for Memory, D for DB)");
                    string dbNumber2 = ask_user_input("Enter DB address of the registers. (0 for Memory address)");
                    string byteNumber2 = ask_user_input("Enter the starting address of the registers");
                    string writeList = ask_user_input("Enter byte values in 8-bit format and put ':' between bytes");

                    address = addressType2 == "M" ? LibnoDaveClass.AddressType.Memory : LibnoDaveClass.AddressType.DB;
                    int.TryParse(dbNumber2, out dbNo);
                    int.TryParse(byteNumber2, out byteNo);
                    writeByteList = new List<string>();
                    writeByteList = writeList.Split(':').ToList();

                    if (libno.write_byte_values(address,dbNo,byteNo, writeByteList))
                    {
                        print_message("Process is successful!");
                    }
                    else
                    {
                        print_message("Some errors occured. Try to connect to PLC again..");
                    }
                    break;

                case "3":
                    string addressType3 = ask_user_input("Which address type do you want to read? (M for Memory, D for DB, I for Input, O for Output)");
                    string dbNumber3 = ask_user_input("Enter DB address of the register. (0 for Memory, Input or Output address)");
                    string byteNumber3 = ask_user_input("Enter the byte address of the register");
                    string bitNumber3 = ask_user_input("Enter the bit address of the register");

                    address = addressType3 == "M" ? LibnoDaveClass.AddressType.Memory : 
                              addressType3 == "D" ? LibnoDaveClass.AddressType.DB : 
                              addressType3 == "I" ? LibnoDaveClass.AddressType.Input : 
                              LibnoDaveClass.AddressType.Output;
                    int.TryParse(dbNumber3, out dbNo);
                    int.TryParse(byteNumber3, out byteNo);
                    int.TryParse(bitNumber3, out bitNo);

                    if (libno.read_bit_value(address, dbNo, byteNo, bitNo, out readBit))
                    {
                        print_message("Process is successful!");
                        print_message("Bit that was read is: " + readBit.ToString());

                    }
                    else
                    {
                        print_message("Some errors occured. Try to connect to PLC again..");
                    }
                    break;

                case "4":
                    string addressType4 = ask_user_input("Which address type do you want to write? (M for Memory, D for DB, O for Output)");
                    string dbNumber4 = ask_user_input("Enter DB address of the register. (0 for Memory or Output address)");
                    string byteNumber4 = ask_user_input("Enter the byte address of the register");
                    string bitNumber4 = ask_user_input("Enter the bit address of the register");
                    string writeBitValue = ask_user_input("Enter the bit value of the register (1 for true, 0 for false)");

                    address = addressType4 == "M" ? LibnoDaveClass.AddressType.Memory :
                              addressType4 == "D" ? LibnoDaveClass.AddressType.DB :
                              LibnoDaveClass.AddressType.Output;
                    int.TryParse(dbNumber4, out dbNo);
                    int.TryParse(byteNumber4, out byteNo);
                    int.TryParse(bitNumber4, out bitNo);
                    writeBit = writeBitValue == "1" ? true : false;

                    if (libno.write_bit_value(address, dbNo, byteNo, bitNo, writeBit))
                    {
                        print_message("Process is successful!");
                    }
                    else
                    {
                        print_message("Some errors occured. Try to connect to PLC again..");
                    }
                    break;

                case "5":
                    string addressType5 = ask_user_input("Which address type do you want to read? (M for Memory, D for DB)");
                    string intType5 = ask_user_input("Which integer type do you want to read? (B for Byte, W for Word or Integer, D for D-Integer)");
                    string dbNumber5 = ask_user_input("Enter DB address of the registers. (0 for Memory address)");
                    string byteNumber5 = ask_user_input("Enter the starting address of the registers");
                    string isSignedStr5 = ask_user_input("Are integer values signed (+/-) ? (y/n)");
                    string lengthNumber5 = ask_user_input("Enter the number of integers");

                    isSigned = isSignedStr5 == "y" ? true : false;
                    address = addressType5 == "M" ? LibnoDaveClass.AddressType.Memory : LibnoDaveClass.AddressType.DB;
                    dataType = intType5 == "B" ? LibnoDaveClass.PLCDataType.Byte :
                               intType5 == "W" ? LibnoDaveClass.PLCDataType.Integer :
                               LibnoDaveClass.PLCDataType.DInteger;
                    int.TryParse(dbNumber5, out dbNo);
                    int.TryParse(byteNumber5, out byteNo);
                    int.TryParse(lengthNumber5, out length);

                    if (libno.read_integer_values(address, dbNo, byteNo, length, out readIntList, dataType, isSigned))
                    {
                        print_message("Process is successful!");
                        print_message("Integers that were read are:");
                        for (int i = 0; i < readIntList.Count; i++)
                        {
                            Console.WriteLine("Int-" + i + " is " + readIntList[i].ToString());
                        }
                    }
                    else
                    {
                        print_message("Some errors occured. Try to connect to PLC again..");
                    }
                    break;

                case "6":
                    string addressType6 = ask_user_input("Which address type do you want to read? (M for Memory, D for DB)");
                    string intType6 = ask_user_input("Which integer type do you want to read? (B for Byte, W for Word or Integer, D for D-Integer)");
                    string dbNumber6 = ask_user_input("Enter DB address of the registers. (0 for Memory address)");
                    string byteNumber6 = ask_user_input("Enter the starting address of the registers");
                    string isSignedStr6 = ask_user_input("Are integer values signed (+/-) ? (y/n)");
                    string writeInts = ask_user_input("Enter int values and put ':' between integers");

                    isSigned = isSignedStr6 == "y" ? true : false;
                    address = addressType6 == "M" ? LibnoDaveClass.AddressType.Memory : LibnoDaveClass.AddressType.DB;
                    dataType = intType6 == "B" ? LibnoDaveClass.PLCDataType.Byte :
                               intType6 == "W" ? LibnoDaveClass.PLCDataType.Integer :
                               LibnoDaveClass.PLCDataType.DInteger;
                    int.TryParse(dbNumber6, out dbNo);
                    int.TryParse(byteNumber6, out byteNo);

                    List<string> writeIntStrList = writeInts.Split(':').ToList();
                    writeIntList = new List<int>();
                    writeIntStrList.ForEach(x => writeIntList.Add(int.Parse(x)));

                    if (libno.write_integer_values(address, dbNo, byteNo, writeIntList, dataType, isSigned))
                    {
                        print_message("Process is successful!");
                    }
                    else
                    {
                        print_message("Some errors occured. Try to connect to PLC again..");
                    }
                    break;

                default:
                    print_message("You entered an unexpected value. Please try again..");
                    break;
            }

        }

        static string ask_user_input(string message)
        {
            string reply = "";
            Console.Write(message + ":   ");
            reply = Console.ReadLine();
            return reply;
        }
        static void print_message(string message)
        {
            Console.WriteLine("");
            Console.WriteLine(message);
            System.Threading.Thread.Sleep(500);
        }
    }
}
