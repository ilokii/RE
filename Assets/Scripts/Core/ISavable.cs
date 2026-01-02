using System;

namespace SaveSystem
{
    /// <summary>
    /// 可保存接口：任何需要保存状态的 Manager 必须实现此接口
    /// </summary>
    public interface ISavable
    {
        /// <summary>
        /// 捕获当前状态快照
        /// </summary>
        /// <returns>需要保存的数据对象（可以是任何可序列化的类型）</returns>
        object CaptureState();

        /// <summary>
        /// 根据传入的数据恢复状态
        /// </summary>
        /// <param name="state">之前保存的状态数据</param>
        void RestoreState(object state);
    }

    /// <summary>
    /// 可保存实体抽象基类（可选）
    /// 提供通用的保存/恢复逻辑框架
    /// </summary>
    public abstract class SaveableEntity : ISavable
    {
        /// <summary>
        /// 实体的唯一标识符（用于在存档中区分不同的实体）
        /// </summary>
        public virtual string EntityId { get; protected set; }

        /// <summary>
        /// 捕获状态的抽象方法，由子类实现具体逻辑
        /// </summary>
        public abstract object CaptureState();

        /// <summary>
        /// 恢复状态的抽象方法，由子类实现具体逻辑
        /// </summary>
        public abstract void RestoreState(object state);

        /// <summary>
        /// 验证状态数据是否有效
        /// </summary>
        protected virtual bool ValidateState(object state)
        {
            return state != null;
        }
    }
}
