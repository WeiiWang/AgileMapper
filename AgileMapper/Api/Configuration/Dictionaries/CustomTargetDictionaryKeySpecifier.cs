namespace AgileObjects.AgileMapper.Api.Configuration.Dictionaries
{
    using System;
    using AgileMapper.Configuration;
    using AgileMapper.Configuration.Dictionaries;
#if FEATURE_DYNAMIC
    using Dynamics;
#endif
    using Members;

    internal class CustomTargetDictionaryKeySpecifier<TSource, TValue> :
        CustomDictionaryKeySpecifierBase<TSource, TValue>,
        ICustomTargetDictionaryKeySpecifier<TSource, TValue>
#if FEATURE_DYNAMIC
        ,
        ICustomTargetDynamicMemberNameSpecifier<TSource>
#endif
    {
        internal CustomTargetDictionaryKeySpecifier(MappingConfigInfo configInfo, QualifiedMember sourceMember)
            : base(configInfo, sourceMember)
        {
        }

        #region Full Keys

        public ITargetDictionaryMappingConfigContinuation<TSource, TValue> ToFullKey(string fullMemberNameKey)
            => RegisterFullMemberNameKey(fullMemberNameKey);

#if FEATURE_DYNAMIC
        public ITargetDynamicMappingConfigContinuation<TSource> ToFullMemberName(string fullMemberName)
            => RegisterFullMemberNameKey(fullMemberName);
#endif

        private DictionaryMappingConfigContinuation<TSource, TValue> RegisterFullMemberNameKey(string fullMemberNameKey)
            => RegisterMemberKey(fullMemberNameKey, (settings, customKey) => settings.AddFullKey(customKey));

        #endregion

        #region Part Keys

        public ITargetDictionaryMappingConfigContinuation<TSource, TValue> ToMemberNameKey(string memberNameKeyPart)
            => RegisterMemberNamePartKey(memberNameKeyPart);

#if FEATURE_DYNAMIC
        public ITargetDynamicMappingConfigContinuation<TSource> ToMemberName(string memberName)
            => RegisterMemberNamePartKey(memberName);
#endif

        private DictionaryMappingConfigContinuation<TSource, TValue> RegisterMemberNamePartKey(string memberNameKeyPart)
        {
            return RegisterMemberKey(memberNameKeyPart, (settings, customKey) => settings.AddMemberKey(customKey));
        }

        #endregion

        private DictionaryMappingConfigContinuation<TSource, TValue> RegisterMemberKey(
            string key,
            Action<DictionarySettings, CustomDictionaryKey> dictionarySettingsAction)
        {
            return RegisterCustomKey(key, dictionarySettingsAction);
        }
    }
}