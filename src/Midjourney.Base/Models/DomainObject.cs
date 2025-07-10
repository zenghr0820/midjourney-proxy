﻿

using System.Runtime.Serialization;
using FreeSql.DataAnnotations;
using Midjourney.Base.Data;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Midjourney.Base.Models
{
    /// <summary>
    /// 基础领域对象类，支持扩展属性和线程同步操作。
    /// </summary>
    //[DataContract] // 由于继承关系，不需要再次标记
    public class DomainObject : IBaseId // , ISerializable
    {
        [JsonIgnore]
        private readonly object _lock = new object();

        private Dictionary<string, object> _properties;

        /// <summary>
        /// 对象ID。
        /// </summary>
        [DataMember]
        [Column(IsPrimary = true)]
        public string Id { get; set; }

        /// <summary>
        /// 暂停当前线程，等待唤醒。
        /// </summary>
        public void Sleep()
        {
            lock (_lock)
            {
                Monitor.Wait(_lock);
            }
        }

        /// <summary>
        /// 唤醒所有等待当前对象锁的线程。
        /// </summary>
        public void Awake()
        {
            lock (_lock)
            {
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// 设置扩展属性。
        /// </summary>
        /// <param name="name">属性名称。</param>
        /// <param name="value">属性值。</param>
        /// <returns>当前对象实例。</returns>
        public DomainObject SetProperty(string name, object value)
        {
            lock (_lock)
            {
                Properties[name] = value;

                // 同时赋值将 Discord 实例 ID  = 频道 ID
                // if (name == Constants.TASK_PROPERTY_DISCORD_INSTANCE_ID)
                // {
                //     Properties[Constants.TASK_PROPERTY_DISCORD_CHANNEL_ID] = value;
                // } 
            }

            return this;
        }

        /// <summary>
        /// 移除扩展属性。
        /// </summary>
        /// <param name="name">属性名称。</param>
        /// <returns>当前对象实例。</returns>
        public DomainObject RemoveProperty(string name)
        {
            lock (_lock)
            {
                Properties.Remove(name); 
            }
            return this;
        }

        /// <summary>
        /// 获取扩展属性值。
        /// </summary>
        /// <param name="name">属性名称。</param>
        /// <returns>属性值。</returns>
        public object GetProperty(string name)
        {
            lock (_lock)
            {
                Properties.TryGetValue(name, out var value);
                return value; 
            }
        }

        /// <summary>
        /// 获取泛型扩展属性值。
        /// </summary>
        /// <typeparam name="T">属性类型。</typeparam>
        /// <param name="name">属性名称。</param>
        /// <returns>属性值。</returns>
        public T GetPropertyGeneric<T>(string name)
        {
            lock (_lock)
            {
                return (T)GetProperty(name); 
            }
        }

        /// <summary>
        /// 获取扩展属性值，并指定默认值。
        /// </summary>
        /// <typeparam name="T">属性类型。</typeparam>
        /// <param name="name">属性名称。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns>属性值或默认值。</returns>
        public T GetProperty<T>(string name, T defaultValue)
        {
            lock (_lock)
            {
                if (Properties.TryGetValue(name, out var value))
                {
                    try
                    {
                        // 检查值是否是目标类型
                        if (value is T t)
                        {
                            return t; // 类型一致，直接返回
                        }

                        // 如果类型不一致，尝试强制转换
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch (InvalidCastException)
                    {
                        // 捕获转换异常，返回默认值
                        return defaultValue;
                    }
                    catch (FormatException)
                    {
                        // 捕获格式异常，返回默认值
                        return defaultValue;
                    }
                    catch (Exception)
                    {
                        return defaultValue;
                    }
                }

                return defaultValue; 
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", Id);
            info.AddValue("Properties", Properties);
        }

        /// <summary>
        /// 获取或初始化扩展属性字典。
        /// </summary>
        //[JsonIgnore]
        [JsonMap]
        public Dictionary<string, object> Properties
        {
            get
            {
                lock (_lock)
                {
                    return _properties ??= new Dictionary<string, object>();
                }
            }
            set
            {
                lock (_lock)
                {
                    _properties = value;
                }
            }
        }

        /// <summary>
        /// 克隆这个对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Clone<T>()
        {
            return (T)MemberwiseClone();
        }
    }
}