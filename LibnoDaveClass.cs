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
    private AddressType all1RType; private int all1DbNo, all1ByteNo, all1BitNo; private bool isallControl, logRequest;
    private bool isConnected, isDisconnected;
    public static byte[] bitSet = new byte[1] { 1 };
    public static byte[] bitReset = new byte[1] { 0 };
    public bool IsConnected { get { return isConnected; } }
    public bool IsDisconnected { get { return isDisconnected; } }

    /// <summary>
    /// <para>Library for reading/writing bits and bytes in Siemens S7-1200 PLCs.</para>
    /// <para> It is possible to read/write bits/bytes at Input(I), Output(O), Memory(M), DataBlock(DB) adresses.</para>
    /// <para>SIEMENS PLC SHOULD BE CONFIGURED TO ENABLE PUT/GET COMMUNICATION.</para>
    /// <para></para>
    /// </summary>
    /// <param name="libnoPort"> Its default value is 102. </param>
    /// <param name="libnoIP"> Ip address of PLC. e.g. "192.168.0.1" </param>
    /// <param name="libnoRack"> Its default value is 0. </param>
    /// <param name="libnoSlot"> Its default value is 1. </param>
    /// <param name="logFileRequest"> If it is true, runtime errors are logged in a file at C:\LibnoDaveClass directory.</param>
    /// <param name="isAlwaysControl"> It is used to increase the relability of the communication by using "Always True Bit" at PLC. If it is true, "Always True Bit" is checked at each read/write operation.</param>
    /// <param name="alwaysTrueAType"> Address type of "Always True Bit" at PLC. If isAlwaysControl is false, this value WILL NOT BE CONSIDERED and there is no need to pass a value. It can be Memory or DB address, BUT IT CANNOT BE INPUT OR OUTPUT ADDRESS!! </param>
    /// <param name="alwaysTrueDbNo"> DB address of "Always True Bit" at PLC. If isAlwaysControl is false, this value WILL NOT BE CONSIDERED and there is no need to pass a value. </param>
    /// <param name="alwaysTrueByteNo"> Byte address of "Always True Bit" at PLC.If isAlwaysControl is false, this value WILL NOT BE CONSIDERED and there is no need to pass a value. </param>
    /// <param name="alwaysTrueBitNo"> Bit address of "Always True Bit" at PLC. If isAlwaysControl is false, this value WILL NOT BE CONSIDERED and there is no need to pass a value. </param>
    public LibnoDaveClass(int libnoPort, string libnoIP, int libnoRack, int libnoSlot, bool logFileRequest, bool isAlwaysControl, AddressType alwaysTrueAType = AddressType.Memory, int alwaysTrueDbNo = 0, int alwaysTrueByteNo = 0, int alwaysTrueBitNo = 0)
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
        logRequest = logFileRequest;
    }


    /// <summary>
    /// This method is used for connecting PLC. The connection status can be checked with "IsConnected" property.
    /// It returns false at unexpected situation.
    /// </summary>
    /// <returns></returns>
    public bool connect_plc()
    {
        bool result = false;
        try
        {
            fds.rfd = libnodave.openSocket(port, ip);
            fds.wfd = fds.rfd;
            di = new libnodave.daveInterface(fds, "FD1", 0, libnodave.daveProtoISOTCP, libnodave.daveSpeed187k);
            di.setTimeout(10000);
            dc = new libnodave.daveConnection(di, 0, rack, slot);
            if (fds.rfd > 0 && dc.connectPLC() == 0)
            {
                result = true;
                isDisconnected = false;
            }
            else
                result = false;
        }
        catch (Exception e)
        {
            send_error_message(e, "CONNECTION PROBLEM");
            result = false;
        }
        isConnected = result;
        return result;
    }

    /// <summary>
    /// This method is used for disconnecting PLC. The connection status can be checked with "IsDisconnected" property.
    /// It returns false at unexpected situation.
    /// </summary>
    /// <returns></returns>
    public bool disconnect_plc()
    {
        bool result = false;
        if (fds.rfd > 0)
        {
            try
            {
                di.disconnectAdapter();
                dc.disconnectPLC();
                libnodave.closePort(fds.wfd);
                result = true;
                isConnected = false;
            }
            catch (Exception e)
            {
                result = false;
                send_error_message(e, "DISCONNECTION PROBLEM");
            }
        }
        isDisconnected = result;
        return result;
    }

    private bool always_bit_check()
    {
        bool result = false;
        byte[] byteRead = new byte[1];
        int valueRead = -1;
        if (isallControl)
        {
            if (all1RType == AddressType.Input || all1RType == AddressType.Output)
            {
                throw new InvalidOperationException("Always True Bit Address cannot be Input or Output address type.");
            }

            int daveInt = (all1RType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
            int daveDB = (all1RType == AddressType.Memory) ? 0 : all1DbNo;
            try
            {
                valueRead = dc.readBits(daveInt, daveDB, all1ByteNo * 8 + all1BitNo, 1, byteRead);
                if (valueRead == 0)
                {
                    if (byteRead[0] == 1) result = true;
                    else result = false;
                }
                else return result;

            }
            catch (Exception e)
            {
            }
        }
        else result = true;

        return result;

    }

    /// <summary>
    /// Address type can be Memory or DB address, BUT IT CANNOT BE INPUT OR OUTPUT ADDRESS!!
    /// It returns false at unexpected situation.
    /// </summary>
    /// <param name="aType">Address type of registers that will be written. AddressType is used.</param>
    /// <param name="dbNo">DB address of registers that will be written. If Address type is Memory, this parameter value is not considered, it can be 0 for the default.</param>
    /// <param name="byteNo">Starting byte address of registers that will be written.</param>
    /// <param name="byteList">Byte values of registers that will be written. It must be a list of strings which represent 8-bit values of each byte in order.
    /// e.g. an element of the list can be "10010101", the first character of the string represents least significant bit.
    /// </param>
    /// <returns></returns>
    public bool write_byte_values(AddressType aType, int dbNo, int byteNo, List<string> byteList)
    {
        bool result = false;
        byte[] readByte = new byte[byteList.Count];
        byte[] byteValue = new byte[byteList.Count];

        if (aType == AddressType.Input || aType == AddressType.Output)
        {
            throw new InvalidOperationException("Address type of registers that will be written cannot be Input or Output address type.");
        }

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
                return result;
            }
            byteValue[i] = outByte;
        }
        int tryCounter = 0;
        int resultValue1 = -1;
        int resultValue2 = -1;

        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
        int daveDB = (aType == AddressType.Memory) ? 0 : dbNo;

        try
        {
            resultValue1 = dc.readBytes(daveInt, daveDB, byteNo, readByte.Length, readByte);
            bool always1Check = always_bit_check();
            if (resultValue1 != 0 || !always1Check)
                return result;
        }
        catch (Exception e)
        {
            send_error_message(e, "WRITE BYTE PROBLEM");
            return result;
        }
        if (!readByte.SequenceEqual(byteValue))
        {
            while (tryCounter < 4)
            {
                try
                {
                    resultValue1 = dc.writeBytes(daveInt, daveDB, byteNo, readByte.Length, byteValue);
                    resultValue2 = dc.readBytes(daveInt, daveDB, byteNo, readByte.Length, readByte);
                    if (readByte.SequenceEqual(byteValue) && resultValue1 == 0 && resultValue2 == 0)
                    {
                        result = true;
                        return result;
                    }
                    else tryCounter += 1;
                }
                catch (Exception e)
                {
                    send_error_message(e, "WRITE BYTE PROBLEM");
                    return result;
                }
            }
        }
        else result = true;
        return result;
    }

    /// <summary>
    /// Address type can be Memory or DB address, BUT IT CANNOT BE INPUT OR OUTPUT ADDRESS!!
    /// It returns false at unexpected situation.
    /// </summary>
    /// <param name="aType">Address type of registers that will be read. AddressType is used.</param>
    /// <param name="dbNo">DB address of registers that will be read. If Address type is Memory, this parameter value is not considered, it can be 0 for the default.</param>
    /// <param name="byteNo">Starting byte address of registers that will be read.</param>
    /// <param name="length">Number of byte registers that will be read.</param>
    /// <param name="resultByte">Byte values of registers that will be read.
    /// Each element represents each byte that is read in order. Each char in each string element represents bit that is read in order.
    /// e.g. Second char of the first element in the list represents second least significant bit of first byte that is read.
    /// If the char is "1", the bit value is true; if the char is "0", the bit value is false.</param>
    /// <returns></returns>
    public bool read_byte_values(AddressType aType, int dbNo, int byteNo, int length, out List<string> resultByte)
    {
        bool result = false;
        int resultValue = -1;
        resultByte = new List<string>();
        List<string> byteList = new List<string>();
        byte[] bytesRead = new byte[length];

        if (aType == AddressType.Input || aType == AddressType.Output)
        {
            throw new InvalidOperationException("Address type of registers that will be read cannot be Input or Output address type.");
        }

        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
        int daveDB = (aType == AddressType.Memory) ? 0 : dbNo;
        try
        {
            resultValue = dc.readBytes(daveInt, daveDB, byteNo, length, bytesRead);
            bool always1Check = always_bit_check();
            if (resultValue == 0 && always1Check)
            {
                foreach (byte byteRead in bytesRead)
                {
                    string stringBit = Convert.ToString(byteRead, 2);
                    string readByte = "00000000";
                    System.Text.StringBuilder readByteNew = new StringBuilder(readByte);
                    for (int i = 0; i < stringBit.Length; i++)
                    {
                        readByteNew[i] = stringBit.Substring(stringBit.Length - 1 - i, 1).ToCharArray()[0];
                    }
                    readByte = readByteNew.ToString();
                    byteList.Add(readByte);
                }

                result = true;
                resultByte = byteList.ToList();
            }

        }
        catch (Exception e) { send_error_message(e, "READ BYTE PROBLEM"); }
        return result;
    }

    /// <summary>
    /// Address type can be Memory or DB address, BUT IT CANNOT BE INPUT OR OUTPUT ADDRESS!!
    /// It returns false at unexpected situation.
    /// </summary>
    /// <param name="aType">Address type of registers that will be read. AddressType is used.</param>
    /// <param name="dbNo">DB address of registers that will be read. If Address type is Memory, this parameter value is not considered, it can be 0 for the default.</param>
    /// <param name="byteNo">Starting byte address of registers that will be read.</param>
    /// <param name="length">Number of integers that will be read.</param>
    /// <param name="resultInts">List that will contain integer values of registers that are read.
    /// Each element represents each integer that is read in order.
    /// </param>
    /// <param name="dataType">Integer type of registers that will be read. PLCDataType is used.
    /// <para>Register type in PLC -> Byte => PLCDataType.Byte</para>
    /// <para>Register type in PLC -> Word, Int(Integer) => PLCDataType.Int</para>
    /// <para>Register type in PLC -> DInt(DInteger) => PLCDataType.DInt</para>
    /// </param>
    /// <param name="isSigned"> If the registers that will be read have sign, this parameter should be true.</param>
    /// <returns></returns>
    public bool read_integer_values(AddressType aType, int dbNo, int byteNo, int length, out List<int> resultInts, PLCDataType dataType = PLCDataType.DInteger, bool isSigned = true)
    {
        bool result = false;
        int resultValue = -1;
        resultInts = new List<int>();
        int byteNum = dataType == PLCDataType.DInteger ? 4 : dataType == PLCDataType.Integer ? 2 : 1;
        int byteLength = length * byteNum;
        byte[] valueRead = new byte[byteLength];

        if (aType == AddressType.Input || aType == AddressType.Output)
        {
            throw new InvalidOperationException("Address type of registers that will be read cannot be Input or Output address type.");
        }

        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
        int daveDB = (aType == AddressType.Memory) ? 0 : dbNo;

        List<int> IntsRead = new List<int>();
        int maxValue = isSigned ? (int)Math.Pow(2, 8 * byteNum - 1) - 1 : (int)Math.Pow(2, 8 * byteNum) - 1; //signed - unsigned int
        try
        {
            resultValue = dc.readBytes(daveInt, daveDB, byteNo, byteLength, valueRead);
            bool always1Check = always_bit_check();
            if (resultValue == 0 && always1Check)
            {
                int readValue = 0;
                for (int i = 0; i < valueRead.Length; i++)
                {
                    readValue += valueRead[i] * (int) Math.Pow(2, 8 * ((valueRead.Length - i - 1) % byteNum));
                    if (i % byteNum == byteNum - 1)
                    {
                        int realValue = (isSigned && readValue > maxValue) ? readValue - 2 * (int)Math.Pow(2, 8 * byteNum - 1) : readValue;
                        IntsRead.Add(realValue);
                        readValue = 0;
                    }
                }
                result = true;
                resultInts = IntsRead.ToList();
            }
        }
        catch (Exception e) { send_error_message(e, "READ INTEGER PROBLEM"); }
        return result;
    }

    /// <summary>
    /// Address type can be Memory or DB address, BUT IT CANNOT BE INPUT OR OUTPUT ADDRESS!!
    /// It returns false at unexpected situation.
    /// </summary>
    /// <param name="aType">Address type of registers that will be written. AddressType is used.</param>
    /// <param name="dbNo">DB address of registers that will be written. If Address type is Memory, this parameter value is not considered, it can be 0 for the default.</param>
    /// <param name="byteNo">Starting byte address of registers that will be written.</param>
    /// <param name="intList">Integer values of registers that will be written. It must be a list of integer in order.</param>
    /// <param name="dataType">Integer type of registers that will be read. PLCDataType is used.
    /// <para>Register type in PLC -> Byte => PLCDataType.Byte</para>
    /// <para>Register type in PLC -> Word, Int(Integer) => PLCDataType.Int</para>
    /// <para>Register type in PLC -> DInt(DInteger) => PLCDataType.DInt</para></param>
    /// <param name="isSigned">If the registers that will be written have sign, this parameter should be true.</param>
    /// <returns></returns>
    public bool write_integer_values(AddressType aType, int dbNo, int byteNo, List<int> intList, PLCDataType dataType = PLCDataType.DInteger, bool isSigned = true)
    {
        bool result = false;
        List<int> readIntList = new List<int>();

        if (aType == AddressType.Input || aType == AddressType.Output)
        {
            throw new InvalidOperationException("Address type of registers that will be written cannot be Input or Output address type.");
        }

        if (!read_integer_values(aType, dbNo, byteNo, intList.Count, out readIntList, dataType))
            return result;

        int byteNum = dataType == PLCDataType.DInteger ? 4 : dataType == PLCDataType.Integer ? 2 : 1;

        int tryCounter = 0;
        int resultValue1 = -1;
        int resultValue2 = -1;

        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
        int daveDB = (aType == AddressType.Memory) ? 0 : dbNo;

        byte[] byteValues = new byte[intList.Count * byteNum];
        byte[] byteValuesRead = new byte[intList.Count * byteNum];
        try
        {
            for (int i = 0; i < intList.Count; i++)
            {
                byte[] intToByteArr;
                if (!convert_byte_values(intList[i], byteNum, isSigned, out intToByteArr)) return result;
                for (int j = 0; j < byteNum; j++)
                {
                    byteValues[i * byteNum + j] = intToByteArr[j];
                }
            }
        } catch(Exception e)
        {
            send_error_message(e, "WRITE INTEGER PROBLEM");
            return result;
        }

        if (!readIntList.SequenceEqual(intList))
        {
            while (tryCounter < 4)
            {
                try
                {
                    resultValue1 = dc.writeBytes(daveInt, daveDB, byteNo, byteValues.Length, byteValues);
                    resultValue2 = dc.readBytes(daveInt, daveDB, byteNo, byteValues.Length, byteValuesRead);
                    if (byteValues.SequenceEqual(byteValuesRead) && resultValue1 == 0 && resultValue2 == 0)
                    {
                        result = true;
                        return result;
                    }
                    else tryCounter += 1;
                }
                catch (Exception e)
                {
                    send_error_message(e, "WRITE INTEGER PROBLEM");
                    return result;
                }
            }
        }
        else result = true;
        return result;
    }
    
    /// <summary>
    /// Address type can be Memory or DB address, BUT IT CANNOT BE INPUT OR OUTPUT ADDRESS!!
    /// It returns false at unexpected situation.
    /// </summary>
    /// <param name="aType">Address type of registers that will be read. AddressType is used.</param>
    /// <param name="dbNo">DB address of registers that will be read. If Address type is Memory, this parameter value is not considered, it can be 0 for the default.</param>
    /// <param name="byteNo">Starting byte address of registers that will be read.</param>
    /// <param name="length">Number of  that will be read.</param>
    /// <param name="resultFloats">List that will contain float values of registers that are read.
    /// Each element represents each float that is read in order.</param>
    /// <returns></returns>
    public bool read_real_values(AddressType aType, int dbNo, int byteNo, int length, out List<float> resultFloats)
    {
        bool result = false;
        int resultValue = -1;
        resultFloats = new List<float>();
        int byteNum = 4;
        int byteLength = length * byteNum;
        byte[] valueRead = new byte[byteLength];

        if (aType == AddressType.Input || aType == AddressType.Output)
        {
            throw new InvalidOperationException("Address type of registers that will be read cannot be Input or Output address type.");
        }

        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
        int daveDB = (aType == AddressType.Memory) ? 0 : dbNo;

        List<float> floatsRead = new List<float>();
        try
        {
            resultValue = dc.readBytes(daveInt, daveDB, byteNo, byteLength, valueRead);
            bool always1Check = always_bit_check();
            if (resultValue == 0 && always1Check)
            {
                for (int i = 0; i < length; i++)
                {
                    byte[] readBytes = new byte[byteNum];
                    for(int j = 0; j < byteNum; j++)
                    {
                        readBytes[j] = valueRead[i * byteNum + j];
                    }
                    Array.Reverse(readBytes);
                    float floatValue = (float) BitConverter.ToSingle(readBytes, 0);
                    floatsRead.Add(floatValue);
                }
                result = true;
                resultFloats = floatsRead.ToList();
            }
        }
        catch (Exception e) { send_error_message(e, "READ FLOAT PROBLEM"); }
        return result;
    }

    /// <summary>
    /// Address type can be Memory or DB address, BUT IT CANNOT BE INPUT OR OUTPUT ADDRESS!!
    /// It returns false at unexpected situation.
    /// </summary>
    /// <param name="aType">Address type of registers that will be written. AddressType is used.</param>
    /// <param name="dbNo">DB address of registers that will be written. If Address type is Memory, this parameter value is not considered, it can be 0 for the default.</param>
    /// <param name="byteNo">Starting byte address of registers that will be written.</param>
    /// <param name="floatList">Float values of registers that will be written. It must be a list of float in order.</param>
    /// <returns></returns>
    public bool write_real_values(AddressType aType, int dbNo, int byteNo, List<float> floatList)
    {
        bool result = false;
        List<float> readFloatList = new List<float>();

        if (aType == AddressType.Input || aType == AddressType.Output)
        {
            throw new InvalidOperationException("Address type of registers that will be written cannot be Input or Output address type.");
        }

        if(!read_real_values(aType, dbNo, byteNo, floatList.Count, out readFloatList))
        {
            return result;
        }

        int byteNum = 4;

        int tryCounter = 0;
        int resultValue1 = -1;
        int resultValue2 = -1;

        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : libnodave.daveDB;
        int daveDB = (aType == AddressType.Memory) ? 0 : dbNo;

        byte[] byteValues = new byte[floatList.Count * byteNum];
        byte[] byteValuesRead = new byte[floatList.Count * byteNum];
        try
        {
            for (int i = 0; i < floatList.Count; i++)
            {
                byte[] floatToByteArr = BitConverter.GetBytes(floatList[i]);
                Array.Reverse(floatToByteArr);

                for (int j = 0; j < byteNum; j++)
                {
                    byteValues[i * byteNum + j] = floatToByteArr[j];
                }
            }
        }
        catch (Exception e)
        {
            send_error_message(e, "WRITE FLOAT PROBLEM");
            return result;
        }

        if (!readFloatList.SequenceEqual(floatList))
        {
            while (tryCounter < 4)
            {
                try
                {
                    resultValue1 = dc.writeBytes(daveInt, daveDB, byteNo, byteValues.Length, byteValues);
                    resultValue2 = dc.readBytes(daveInt, daveDB, byteNo, byteValues.Length, byteValuesRead);
                    if (byteValues.SequenceEqual(byteValuesRead) && resultValue1 == 0 && resultValue2 == 0)
                    {
                        result = true;
                        return result;
                    }
                    else tryCounter += 1;
                }
                catch (Exception e)
                {
                    send_error_message(e, "WRITE FLOAT PROBLEM");
                    return result;
                }
            }
        }
        else result = true;
        return result;
    }

    /// <summary>
    /// Address type can be Memory, Output or DB address, BUT IT CANNOT BE INPUT ADDRESS!!
    /// It returns false at unexpected situation.
    /// </summary>
    /// <param name="aType">Address type of register that will be written. AddressType is used.</param>
    /// <param name="dbNo">DB address of register that will be written. If Address type is Memory or Output, this parameter value is not considered, it can be 0 for the default.</param>
    /// <param name="byteNo">Byte address of register that will be written.</param>
    /// <param name="bitNo">Bit address of register that will be written.</param>
    /// <param name="bitValue">Value of register that will be written.</param>
    /// <param name="feedbackMode">If it is true, register value will be read to check the written value.</param>
    /// <returns></returns>
    public bool write_bit_value(AddressType aType, int dbNo, int byteNo, int bitNo, bool bitValue, bool feedbackMode = true)
    {
        bool result = false;
        byte[] byteValue = bitValue ? bitSet : bitReset;
        byte[] byteRead = new byte[byteValue.Length];
        int tryCounter = 0;
        int valueRead = -1;
        int daveInt = aType == AddressType.Memory ? libnodave.daveFlags : aType == AddressType.Output ? libnodave.daveOutputs : libnodave.daveDB;
        int daveDB = (aType == AddressType.Memory || aType == AddressType.Output) ? 0 : dbNo;

        if(aType == AddressType.Input)
        {
            throw new InvalidOperationException("Address type of register that will be written cannot be Input address type.");
        }

        try
        {
            valueRead = dc.readBits(daveInt, daveDB, byteNo * 8 + bitNo, 1, byteRead);
            bool always1Check = always_bit_check();
            if (valueRead != 0 || !always1Check)
                return result;
        }
        catch (Exception e)
        {
            send_error_message(e, "WRITE BIT PROBLEM");
            return result;
        }
        if (!byteRead.SequenceEqual(byteValue))
        {
            while (tryCounter < 4)
            {
                try
                {
                    valueRead = dc.writeBits(daveInt, daveDB, byteNo * 8 + bitNo, 1, byteValue);
                    if (valueRead == 0)
                    {
                        if (feedbackMode == false)
                        {
                            for (int i = 0; i < byteValue.Length; i++)
                            {
                                byteRead[i] = byteValue[i];
                            }
                        }
                        else
                        {
                            valueRead = dc.readBits(daveInt, daveDB, byteNo * 8 + bitNo, 1, byteRead);
                            if (valueRead != 0)
                            {
                                tryCounter += 1;
                                continue;
                            }
                        }

                        if (byteRead.SequenceEqual(byteValue))
                        {
                            result = true;
                            return result;
                        }
                        else tryCounter += 1;
                    }
                    else tryCounter += 1;

                }
                catch (Exception e)
                {
                    send_error_message(e, "WRITE BIT PROBLEM");
                    return result;
                }
            }
        }
        else result = true;
        return result;
    }

    /// <summary>
    /// It returns false at unexpected situation.
    /// </summary>
    /// <param name="aType">Address type of register that will be written. AddressType is used.</param>
    /// <param name="dbNo">DB address of register that will be written. If Address type is Memory, Input or Output, this parameter value is not considered, it can be 0 for the default.</param>
    /// <param name="byteNo">Byte address of register that will be read.</param>
    /// <param name="bitNo">Bit address of register that will be read.</param>
    /// <param name="bitValue">Value of register that will be read.</param>
    /// <returns></returns>
    public bool read_bit_value(AddressType aType, int dbNo, int byteNo, int bitNo, out bool bitValue)
    {
        bool result = false;
        bitValue = false;
        byte[] readByte = new byte[1];
        int readValue = -1;
        int daveInt = (aType == AddressType.Memory) ? libnodave.daveFlags : (aType == AddressType.Input ? libnodave.daveInputs : (aType == AddressType.Output ? libnodave.daveOutputs : libnodave.daveDB));
        int daveDB = (aType == AddressType.Memory || aType == AddressType.Input || aType == AddressType.Output) ? 0 : dbNo;
        try
        {
            readValue = dc.readBits(daveInt, daveDB, byteNo * 8 + bitNo, 1, readByte);
            bool always1Check = always_bit_check();
            if (readValue == 0 && always1Check)
            {
                if (readByte[0] == 1) bitValue = true;
                else bitValue = false;
                result = true;
            }
            else return result;

        }
        catch (Exception e)
        {
            send_error_message(e, "READ BIT PROBLEM");
        }
        return result;
    }

    private string reverse_string(string str)
    {
        char[] resultArr = str.ToCharArray();
        Array.Reverse(resultArr);
        return new string(resultArr);
    }

    private bool convert_byte_values(int intValue, int byteLength, bool isSigned, out byte[] convertByte)
    {
        bool result = false;
        convertByte = new byte[byteLength];
        int maxValue = isSigned ? (int)Math.Pow(2, 8 * byteLength - 1) - 1 : (int) Math.Pow(2, 8 * byteLength) - 1 ; //signed - unsigned int
        if (Math.Abs(intValue) > maxValue || (!isSigned && intValue < 0))
        {
            return result;
        }
        else if(intValue < 0)
        {
            convertByte[byteLength - 1] = (byte) Math.Pow(2, 7);
        }
        else
        {
            convertByte[byteLength - 1] = 0;
        }
        int reversedValue = intValue < 0 ? maxValue + 1 - Math.Abs(intValue) : intValue;

        for (int i = byteLength - 1; i >= 0; i--)
        {
            if(i == byteLength - 1)
            {
                convertByte[i] += (byte) (reversedValue / (int) Math.Pow(2, 8 * i));
            }
            else
            {
                convertByte[i] = (byte) ((reversedValue % (int)Math.Pow(2, 8 * (i + 1))) / (int)Math.Pow(2, 8 * i));
            }
        }
        Array.Reverse(convertByte);
        result = true;

        return result;

    }
    private void send_error_message(Exception e, string errorAddress)
    {
        if (logRequest)
        {
            string errorLog = " THE BLOCK WHICH ERROR OCCURED: " + errorAddress
                            + "  ERROR DESCRIPTION: " + e.GetType().ToString() + "   " + e.Message;
            create_log_file("libno_error_log.txt");
            write_log_to_file("libno_error_log.txt", errorLog);
        }
    }
    private void create_log_file(string fileName)
    {
        Directory.CreateDirectory("C:\\LibnoDaveClass");
        string[] getFiles = Directory.GetFiles("C:\\LibnoDaveClass", fileName);
        if (getFiles.Count() == 0)
        {
            File.Create("C:\\LibnoDaveClass\\" + fileName);
        }
    }
    private void write_log_to_file(string fileName, string log)
    {
        try
        {
            string path = "C:\\LibnoDaveClass\\" + fileName;
            if (File.Exists(path))
            {
                FileInfo varolanDosya = new FileInfo(path);
                if (varolanDosya.Length > 10000000) //greater than 10mb
                    File.Delete(path);
            }
            using (StreamWriter dosyaYaz = new StreamWriter(path, true))
            {
                dosyaYaz.WriteLine(log + "        " + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss"));
                dosyaYaz.Close();
            }
        }
        catch (Exception e) {}
    }

}