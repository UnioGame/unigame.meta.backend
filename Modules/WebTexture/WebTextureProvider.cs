namespace UniGame.MetaBackend.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UniCore.Runtime.ProfilerTools;
    using Cysharp.Threading.Tasks;
    using UniModules.Runtime.Network;
    using global::UniGame.Runtime.DataFlow;
    using global::UniGame.Runtime.Rx;
    using MetaService.Runtime;
    using R3;
    using UniGame.Core.Runtime;
    using UniGame.MetaBackend.Shared;
    using UniGame.MetaBackend.Runtime;
     
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Serializable]
    public class WebTextureProvider : IWebTextureProvider
    {
        public const string NotSupportedError = "Not supported";
        public const string Scheme = "http";
        private const double FailureRetryDelaySeconds = 60;
        
        private WebTextureSettings _settings;
        private ILifeTime _defaultLifeTime;
        private WebRequestBuilder _webRequestBuilder;
        private ReactiveValue<ConnectionState> _state = new(ConnectionState.Connected);
        private LifeTime _lifeTime = new();
        private Dictionary<string,TextureCacheItem> _cache = new();
        private readonly Dictionary<string, (string error, double retryAfter)> _failedRequests = new();

        public ILifeTime LifeTime => _lifeTime;
        public ReadOnlyReactiveProperty<ConnectionState> State => _state;
        
        public WebTextureProvider(WebTextureSettings settings,ILifeTime defaultLifeTime)
        {
            _webRequestBuilder = new WebRequestBuilder();
            _settings = settings;
            _defaultLifeTime = defaultLifeTime;
            
            UpdateCacheAsync().Forget();
        }

        public void SetToken(string token)
        {
            _webRequestBuilder.SetToken(token);
        }
        
        public UniTask<MetaConnectionResult> ConnectAsync()
        {
            return UniTask.FromResult(new MetaConnectionResult()
            {
                State = _state.Value,
                Error = string.Empty,
                Success = true,
            });
        }

        public UniTask DisconnectAsync()
        {
            return UniTask.CompletedTask;
        }
        
        public bool IsContractSupported(IRemoteMetaContract command)
        {
            return command is WebSpriteContract or WebTexture2DContract;
        }

        public async UniTask<ContractMetaResult> ExecuteAsync(MetaContractData contractData,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new ContractMetaResult()
            {
                data = null,
                error = NotSupportedError,
                success = false,
                id = contractData.contractName,
            };
            
            var contract = contractData.contract;
            var path = contract.Path;
            var isUrl = path.StartsWith(Scheme);
            var url = isUrl
                ? path
                : _settings.url.MergeUrl(path);

            url = url.UpdateUrlPattern(contract);
            
            var name = isUrl ? string.Empty : path;

#if UNITY_EDITOR
            if (isUrl && string.IsNullOrEmpty(name))
            {
                var startIndex = url.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                var nameIndex = startIndex + 1;
                if(startIndex >= 0 && nameIndex < url.Length)
                    name = url.Substring(nameIndex);
            }
#endif
            
            var lifeTimeContext = contract as ILifeTimeContext;
            var resourceLifeTime = lifeTimeContext?.LifeTime ?? _defaultLifeTime;
            var outputType = contract.OutputType;
            
            if (TryGetFailure(url, out var error))
            {
                result.error = error;
                return result;
            }

            // The download is shared by all consumers; cancelling one waiter must not abort it.
            var cached = await LoadFromCacheAsync(url,outputType,resourceLifeTime)
                .AttachExternalCancellation(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            
            if (cached.success)
            {
                result.data = cached.asset;
                result.success = true;
                result.error = string.Empty;
                return result;
            }

            if (TryGetFailure(url, out error))
            {
                result.error = error;
                return result;
            }
            
            if (outputType == typeof(Sprite))
            {
                var spriteResult = await LoadWebSpriteAsync(url,name,resourceLifeTime)
                    .AttachExternalCancellation(cancellationToken);
                
                result.data = spriteResult.sprite;
                result.success = spriteResult.success;
                result.error = spriteResult.error;
            }

            if (outputType == typeof(Texture2D))
            {
                var textureResult = await LoadWebTextureAsync(url,name,resourceLifeTime)
                    .AttachExternalCancellation(cancellationToken);
                var texture = textureResult.texture;
                result.data = texture;
                result.success = textureResult.success;
                result.error = textureResult.error;
            }
            
            return result;
        }

        public bool TryDequeue(out ContractMetaResult result)
        {
            result = default;
            return false;
        }

        public async UniTask<WebServerSpriteResult> LoadWebSpriteAsync(string url,string name,ILifeTime lifeTime)
        {
            var result = new WebServerSpriteResult()
            {
                sprite = null,
                error = string.Empty,
                success = false,
            };

            if (TryGetFailure(url, out var error))
            {
                result.error = error;
                return result;
            }
            
            var cacheItem = AddToCache(url,name,lifeTime);

            if (cacheItem.sprite != null)
            {
                result.sprite = cacheItem.sprite;
                result.success = true;
                return result;
            }

            var texture = cacheItem.texture;
            if (cacheItem.texture != null)
            {
                var createdSprite = CreateSpriteByTexture(cacheItem,texture);
                result.sprite = createdSprite;
                result.success = true;
                return result;
            }
            
            try
            {
                result = await _webRequestBuilder.GetSpriteAsync(url, logErrors: false,
                    cancellation: _lifeTime.Token);
                cacheItem.sprite = result.sprite;
                if (!result.success)
                    RecordFailure(url, result.error);
            }
            finally
            {
                cacheItem.loaded = true;
            }
            
            return result;
        }
        
        public async UniTask<WebServerTexture2DResult> LoadWebTextureAsync(string url,string name,ILifeTime lifeTime)
        {
            var result = new WebServerTexture2DResult()
            {
                texture = null,
                error = string.Empty,
                success = false,
            };

            if (TryGetFailure(url, out var error))
            {
                result.error = error;
                return result;
            }
            
            var cacheItem = AddToCache(url,name,lifeTime);

            if (cacheItem.texture != null)
            {
                result.texture = cacheItem.texture;
                result.success = true;
                return result;
            }

            if (cacheItem.sprite != null)
            {
                result.texture = cacheItem.sprite.texture;
                result.success = true;
                return result;
            }
            
            try
            {
                result = await _webRequestBuilder.GetTextureAsync(url, logErrors: false,
                    cancellation: _lifeTime.Token);
                if (result.success)
                {
                    result.texture.name = name;
                    cacheItem.texture = result.texture;
                }
                else
                {
                    RecordFailure(url, result.error);
                }
            }
            finally
            {
                cacheItem.loaded = true;
            }
            return result;
        }

        private bool TryGetFailure(string url, out string error)
        {
            error = string.Empty;
            if (!_failedRequests.TryGetValue(url, out var failure))
                return false;
            if (Time.realtimeSinceStartupAsDouble >= failure.retryAfter)
            {
                _failedRequests.Remove(url);
                return false;
            }

            error = failure.error;
            return true;
        }

        private void RecordFailure(string url, string error)
        {
            if (TryGetFailure(url, out _))
                return;

            error = string.IsNullOrEmpty(error) ? "Failed to load web image" : error;
            _failedRequests[url] = (error, Time.realtimeSinceStartupAsDouble + FailureRetryDelaySeconds);
            GameLog.LogWarning($"Web image unavailable: {url} | {error}");
        }
        
        public TextureCacheItem AddToCache(string url,string name, ILifeTime lifeTime)
        {
            if(_cache.TryGetValue(url,out var item))
            {
                item.counter++;
                lifeTime.AddDispose(item);
                return item;
            }
            
            var cacheItem = new TextureCacheItem
            {
                counter = 1,
                name = name,
                texture = null,
                sprite = null,
                url = url,
                isAlive = true,
                loaded = false,
            }.AddTo(lifeTime);

            _cache[url] = cacheItem;
            return cacheItem;
        }
        
        public async UniTask<TextureCacheResult> LoadFromCacheAsync(string url,Type assetType,ILifeTime lifeTime)
        {
            var result = new TextureCacheResult();
            if (!_cache.TryGetValue(url, out var item))
                return result;
            
            if(item.isAlive == false)
                return result;
            
            item.counter++;
            lifeTime.AddDispose(item);
            
            if(!item.loaded) {
                await UniTask.WaitWhile(item,x => x.loaded == false)
                .AttachExternalCancellation(lifeTime.Token);
            }
            
            if (assetType == typeof(Texture2D))
            {
                if (item.texture != null)
                {
                    result.asset = item.texture;
                    result.success = true;
                    return result;
                }
                
                if (item.sprite == null) return new TextureCacheResult();
                
                item.texture = item.sprite.texture;
                item.texture.name = item.name;
                
                result.asset = item.texture;
                result.success = true;
                return result;
            }
            
            if (assetType == typeof(Sprite))
            {
                if (item.sprite != null)
                {
                    result.asset = item.sprite;
                    result.success = true;
                    return result;
                }
                if (item.texture == null) return result;
                var texture = item.texture;
                
                var sprite = CreateSpriteByTexture(item,texture);
                
                result.asset = sprite;
                result.success = true;
                return result;
            }

            return result;
        }
        
        public Sprite CreateSpriteByTexture(TextureCacheItem item,Texture2D texture)
        {
            if(item.sprite!=null) return item.sprite;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            sprite.name = item.name;
            item.sprite = sprite;
            return sprite;
        }
        
        public void Dispose()
        {
            _lifeTime.Terminate();
        }

        private async UniTask UpdateCacheAsync()
        {
            var remotedIds = new List<string>();
            while (!_lifeTime.IsTerminated)
            {
                remotedIds.Clear();
                
                foreach (var pair in _cache)
                {
                    var item = pair.Value;
                    if (item.counter <= 0 || item.isAlive == false)
                        remotedIds.Add(pair.Key);
                }

                foreach (var id in remotedIds)
                {
                    if (!_cache.TryGetValue(id, out var item))
                        continue;

                    item.CleanUp();
                    _cache.Remove(id);
                }

                remotedIds.Clear();
                foreach (var pair in _failedRequests)
                {
                    if (Time.realtimeSinceStartupAsDouble >= pair.Value.retryAfter)
                        remotedIds.Add(pair.Key);
                }
                foreach (var id in remotedIds)
                    _failedRequests.Remove(id);
                
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: _lifeTime.Token);
            }
        }

    }
}
