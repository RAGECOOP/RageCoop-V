﻿using System;
using System.Collections.Generic;
using System.Text;
using GTA.Math;
using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {

        internal class BulletShot : Packet
        {
            public override PacketType Type  => PacketType.BulletShot;
            public int OwnerID { get; set; }

            public uint WeaponHash { get; set; }

            public Vector3 StartPosition { get; set; }
            public Vector3 EndPosition { get; set; }

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                // Write OwnerID 
                byteArray.AddRange(BitConverter.GetBytes(OwnerID));

                // Write weapon hash
                byteArray.AddRange(BitConverter.GetBytes(WeaponHash));

                // Write StartPosition
                byteArray.AddVector3(StartPosition);

                // Write EndPosition
                byteArray.AddVector3(EndPosition);


                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read OwnerID
                OwnerID=reader.ReadInt();

                // Read WeponHash
                WeaponHash=reader.ReadUInt();

                // Read StartPosition
                StartPosition=reader.ReadVector3();

                // Read EndPosition
                EndPosition=reader.ReadVector3();
                #endregion
            }
        }




    }
}
