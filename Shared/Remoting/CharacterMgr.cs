﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Shared.Database;

namespace Shared
{
    [RpcAttributes(new string[] { "CharacterServer" , "WorldServer" })]
    public class CharacterMgr : ARpc
    {
        static public ObjectDatabase CharacterDB = null;
        static public CharacterMgr Instance = null;

        public CharacterMgr()
        {
            if (Instance == null)
                Instance = this;
        }

        #region Realms

        static public Dictionary<int, Realm> _Realms = new Dictionary<int, Realm>();

        public void LoadRealms()
        {
            _Realms.Clear();

            IList<Realm> Rms = CharacterDB.SelectAllObjects<Realm>();

            foreach (Realm Rm in Rms)
            {
                Rm.GenerateName();
                Rm.Online = 0;
                Rm.Dirty = true;
                _Realms.Add(Rm.RealmId, Rm);
                CharacterDB.SaveObject(Rm);
            }

            Log.Success("LoadRealms", "Loaded " + _Realms.Count + " Realm(s)");
        }
        public Realm GetRealm(byte RealmId)
        {
            return GetRealms().ToList().Find(info => info.RealmId == RealmId);
        }
        public Realm GetRealm(int RiftId)
        {
            return GetRealms().ToList().Find(info => info.RiftId == RiftId);
        }
        public Realm[] GetRealms()
        {
            return _Realms.Values.ToArray();
        }

        public Realm RegisterRealm(byte RealmId,string RealmIP,int RealmPort,int RpcId)
        {
            Realm Rm = GetRealm(RealmId);
            if (Rm == null)
                return Rm;

            Rm.Address = RealmIP+":"+RealmPort;
            Rm.Online = 1;
            Rm.RpcId = RpcId;
            Rm.Dirty = true;

            Log.Success("Realm", "World (" + Rm.Name + ") Online at : " + Rm.Address);

            SaveObject(Rm);
            return Rm;
        }

        #endregion

        #region Characters

        public Character GetCharacter(int CharacterId)
        {
            return CharacterDB.SelectObject<Character>("Id='" + CharacterId + "'");
        }
        public Character GetCharacter(string Name)
        {
            return CharacterDB.SelectObject<Character>("Name='" + CharacterDB.Escape(Name) + "'");
        }
        public Character GetCharacter(string Name, byte RealmId)
        {
            return CharacterDB.SelectObject<Character>("Name='" + CharacterDB.Escape(Name) + "' AND RealmId='" + RealmId + "'");
        }

        public int GetCharactersCount(long AccountId, byte RealmId)
        {
            return CharacterDB.GetObjectCount<Character>("AccountId=" + AccountId + " AND RealmId=" + RealmId);
        }
        public Character[] GetCharacters(long AccountId,byte RealmId)
        {
            return CharacterDB.SelectObjects<Character>("AccountId='" + AccountId + "' AND RealmId='" + RealmId + "'").ToArray();
        }
        public Character[] GetCharacters(byte RealmId)
        {
            return CharacterDB.SelectObjects<Character>("RealmId='" + RealmId + "'").ToArray();
        }
        public Character[] GetCharacters(string Name)
        {
            return CharacterDB.SelectObjects<Character>("Name='" + CharacterDB.Escape(Name) + "'").ToArray();
        }
        public Model_Info GetItemModel(long ModelID, long Race, long Sex)
        {
            return CharacterDB.SelectObject<Model_Info>("ModelID=" + ModelID + " AND Race=" + Race + " AND Sex=" + Sex + "");
        }
        public Items_Template GetItemTemplate(long ItemID)
        {
            return CharacterDB.SelectObject<Items_Template>("Id=" + ItemID);
        }
        public Model_Info GetModelForCacheID(long CacheID)
        {
            return CharacterDB.SelectObject<Model_Info>("CacheID=" + CacheID);
        }
        public Character_StartItems[] GetStartItems(long Race, long Sex, long Class)
        {
            return CharacterDB.SelectObjects<Character_StartItems>("(Race=" + Race + " OR Race= -1) AND (Sex=" + Sex + " OR Sex= -1) AND (Class=" + Class + " OR Class= -1)").ToArray();
        }
        public Character_Item[] GetPlayerItems(long GUID)
        {
            return CharacterDB.SelectObjects<Character_Item>("GUID=" + GUID).ToArray();
        }
        public Items_Template[] GetEquipedItems(long GUID)
        {
            Character_Item[] Items = CharacterDB.SelectObjects<Character_Item>("GUID=" + GUID + " AND Equiped=1").ToArray();
            Items_Template[] Ret = new Items_Template[Items.Length];

            for (int i = 0; i < Items.Length; i++)
                Ret[i] = GetItemTemplate(Items[i].ItemID);

            return Ret;
        }
        public RaceSexMask_Info GetMaskForRaceSex(long Race, long Sex)
        {
            return CharacterDB.SelectObject<RaceSexMask_Info>("Race=" + Race + " AND Sex=" + Sex + "");
        }

        public void AddObject(DataObject Char)
        {
            CharacterDB.AddObject(Char);
        }
        public void SaveObject(DataObject Char)
        {
            Char.Dirty = true;
            CharacterDB.SaveObject(Char);
        }
        public void RemoveObject(DataObject Char)
        {
            CharacterDB.DeleteObject(Char);
        }

        static public List<Creation_Name> _Randoms = new List<Creation_Name>();

        public void LoadCreation_Names()
        {
            _Randoms.AddRange(CharacterDB.SelectAllObjects<Creation_Name>());

            Log.Success("LoadCreation_Names", "Loaded " + _Randoms.Count + " Random Name(s)");
        }

        public string GenerateName()
        {
            if (_Randoms.Count <= 0)
                return "Siennacore";

            Random R = new Random();
            int Id = R.Next(_Randoms.Count);

            return _Randoms[Id].Name;
        }

        #endregion

        #region CacheData

        public byte[] GetCache(long CacheType, uint ID)
        {
            try
            {
                FileStream Ft = new FileStream("CacheData/" + CacheType + "-" + ID + ".cache", FileMode.Open);
                if (Ft == null || !Ft.CanRead)
                {
                    Log.Error("GetCache", "Invalid Cache Data : Type=" + CacheType + ",ID=" + ID);
                    return null;
                }

                byte[] Result = new byte[Ft.Length];
                Ft.Read(Result, 0, (int)Result.Length);
                Ft.Close();
                return Result;
            }
            catch
            {
                Log.Error("GetCache", "Invalid Cache Data : Type=" + CacheType + ",ID=" + ID);
                return null;
            }
        }

        public byte[] GetBuild(long GUID)
        {
            try
            {
                FileStream Ft = new FileStream("BuildData/" + GUID + ".cache", FileMode.Open);
                if (Ft == null || !Ft.CanRead)
                {
                    Log.Error("GetBuild", "Invalid Build Data : GUID=" + GUID);
                    return null;
                }

                byte[] Result = new byte[Ft.Length];
                Ft.Read(Result, 0, (int)Result.Length);
                Ft.Close();
                return Result;
            }
            catch
            {
                Log.Error("GetCache", "Invalid Build Data : GUID=" + GUID);
                return null;
            }
        }

        #endregion

        public override void Disconnected(int Id)
        {
            Realm[] Rms = GetRealms();
            foreach (Realm Rm in Rms)
            {
                if (Rm.RpcId == Id)
                {
                    Log.Error("Realm", "World (" + Rm.Name + ") Offline at : " + Rm.Address);
                    Rm.Online = 0;
                    Rm.RpcId = 0;
                    Rm.Dirty = true;
                    SaveObject(Rm);
                }
            }
            base.Disconnected(Id);
        }
    }
}
