using System.Collections.Generic;
using UnityEngine;
using XLHFramework.GCFrameWork.Base;

namespace XLHFramework.GCFrameWork.World
{
    public partial class World
    {
        /// <summary>
        /// 逻辑层所有类的一个字典
        /// </summary>
        private static Dictionary<string, ILogicBehaviour> mLogicBehaviourDic = new Dictionary<string, ILogicBehaviour>();
        /// <summary>
        /// 数据层所有类的一个字典
        /// </summary>
        private static Dictionary<string, IDataBehaviour> mDataBehaviourDic = new Dictionary<string, IDataBehaviour>();
        /// <summary>
        /// 消息层所有类的一个字典
        /// </summary>
        private static Dictionary<string, IMsgBehaviour> mMsgBehaviourDic = new Dictionary<string, IMsgBehaviour>();
        
        /// <summary>
        /// 世界构建初触发
        /// </summary>
        public virtual void OnCreate() { }

        public virtual void OnUpdate() { }
        /// <summary>
        /// 世界销毁时触发
        /// </summary>
        public virtual void OnDestroy() { }
        
        
        /// <summary>
        /// 销毁游戏世界
        /// </summary>
        public void DestroyWorld(string nameSpace, object pars = null)
        {
            //需要移除的一个列表
            List<string> needRemoveList = new List<string>();

            //释放逻辑层脚本
            foreach (var item in mLogicBehaviourDic)
            {
                if (string.Equals(item.Value.GetType().Namespace, nameSpace))
                    needRemoveList.Add(item.Key);
            }
            foreach (var key in needRemoveList)
            {
                mLogicBehaviourDic[key].OnDestroy();
                mLogicBehaviourDic.Remove(key);
            }

            //释放数据层脚本
            needRemoveList.Clear();
            foreach (var item in mDataBehaviourDic)
            {
                if (string.Equals(item.Value.GetType().Namespace, nameSpace))
                    needRemoveList.Add(item.Key);
            }
            foreach (var key in needRemoveList)
            {
                mDataBehaviourDic[key].OnDestroy();
                mDataBehaviourDic.Remove(key);
            }

            //释放消息层脚本
            needRemoveList.Clear();
            foreach (var item in mMsgBehaviourDic)
            {
                if (string.Equals(item.Value.GetType().Namespace, nameSpace))
                    needRemoveList.Add(item.Key);
            }
            foreach (var key in needRemoveList)
            {
                mMsgBehaviourDic[key].OnDestroy();
                mMsgBehaviourDic.Remove(key);
            }

            OnDestroy();

        }
        
        /// <summary>
        /// 世界销毁完成后触发
        /// </summary>
        /// <param name="args"></param>
        public virtual void OnDestroyPostProcess(object args) { }
        
        /// <summary>
        /// 获取逻辑层控制器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetExitsLogicCtrl<T>() where T : ILogicBehaviour
        {
            if (mLogicBehaviourDic.TryGetValue(typeof(T).Name, out var logic))
            {
                return (T)logic;
            }
            Debug.LogError($"{typeof(T).Name}Not Get class Failed! Please check Params");
            return default(T);
        }
        /// <summary>
        /// 获取数据管理器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetExitsDataMgr<T>() where T : IDataBehaviour
        {
            if (mDataBehaviourDic.TryGetValue(typeof(T).Name, out var data))
            {
                return (T)data;
            }
            Debug.LogError($"{typeof(T).Name}Not Get class Failed! Please check Params");
            return default(T);
        }
        /// <summary>
        /// 获取消息层管理器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetExitsMsgMgr<T>() where T : IMsgBehaviour
        {
            if (mMsgBehaviourDic.TryGetValue(typeof(T).Name, out var msg))
            {
                return (T)msg;
            }
            Debug.LogError($"{typeof(T).Name}Not Get class Failed! Please check Params");
            return default(T);
        }
    }
}