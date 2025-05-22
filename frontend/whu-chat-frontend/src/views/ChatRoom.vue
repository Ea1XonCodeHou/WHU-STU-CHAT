<template>
  <div class="chat-room-container">
    <!-- 头部信息 -->
    <header class="chat-header">
      <div class="room-info">
        <h1>{{ roomName }}</h1>
        <div class="room-status">
          <span class="status-indicator" :class="{ 'connected': isConnected }"></span>
          <span class="status-text">{{ connectionStatus }}</span>
          <span class="online-count">在线用户: {{ onlineUsers.length }}</span>
        </div>
      </div>
      <div class="user-info">
        <span class="username">{{ username }}</span>
        <div class="avatar" v-if="userAvatar">
          <img :src="userAvatar" alt="用户头像" />
        </div>
        <div class="avatar default-avatar" v-else>
          {{ username.charAt(0).toUpperCase() }}
        </div>
        <button class="leave-button" @click="leaveRoom">退出聊天室</button>
      </div>
    </header>

    <!-- 主内容区 -->
    <main class="chat-main">
      <!-- 聊天消息区 -->
      <div class="messages-container" ref="messagesContainer">
        <div class="messages-wrapper">
          <div v-if="loadingHistory" class="loading-message">
            <div class="loading-spinner"></div>
            <span>加载历史消息中...</span>
          </div>
          
          <div v-for="(message, index) in messages" :key="message.messageId || index" 
               class="message-item" 
               :class="getMessageClass(message)">
            
            <!-- 日期分隔线 -->
            <div v-if="shouldShowDateSeparator(index)" class="date-separator">
              <span>{{ formatDate(message.sendTime) }}</span>
            </div>
            
            <!-- 系统消息 -->
            <div v-if="message.messageType === 'system'" class="system-message">
              <div class="system-message-content">
                <i class="system-icon"></i>
                <span>{{ message.content }}</span>
              </div>
              <div class="message-time">{{ formatTime(message.sendTime) }}</div>
            </div>
            
            <!-- 用户消息 -->
            <div v-else class="user-message" :class="{'self-message': message.senderId === userId}">
              <!-- 统一的头像显示区域 (作为第一个子元素) -->
              <div class="message-avatar"
                   :class="{'self-avatar': message.senderId === userId}"
                   @click.stop="message.senderId !== userId ? showUserCard(message.senderId) : null">
                <template v-if="message.senderId === userId">
                  <!-- 自己的头像 -->
                  <img v-if="userAvatar" :src="userAvatar" alt="用户头像" class="avatar-image" @error="handleImageError" />
                  <div v-else class="default-avatar">
                    {{ username.charAt(0).toUpperCase() }}
                  </div>
                </template>
                <template v-else>
                  <!-- 其他用户的头像 -->
                  <img v-if="getUserAvatar(message.senderId)" :src="getUserAvatar(message.senderId)" alt="用户头像" class="avatar-image" @error="handleImageError" />
                  <div v-else class="default-avatar">
                    {{ message.senderName ? message.senderName.charAt(0).toUpperCase() : '?' }}
                  </div>
                </template>
              </div>
              
              <!-- 消息内容 (作为第二个子元素) -->
              <div class="message-content">
                <div class="message-info">
                  <span class="message-sender" v-if="message.senderId !== userId" @click.stop="showUserCard(message.senderId)">{{ message.senderName }}</span>
                  <span class="message-time">{{ formatTime(message.sendTime) }}</span>
                </div>
                
                <!-- 消息类型内容 -->
                <div v-if="message.messageType === 'text'" class="message-text">
                  {{ message.content }}
                </div>
                
                <div v-else-if="message.messageType === 'image'" class="message-image">
                  <img :src="getFullImageUrl(message.fileUrl)" alt="图片消息" @click="previewImage(message.fileUrl)" @error="handleImageError" />
                  <div class="image-info">{{ message.fileName }} ({{ formatFileSize(message.fileSize) }})</div>
                </div>
                
                <div v-else-if="message.messageType === 'file'" class="message-file" @click="downloadFile(message.fileUrl, message.fileName)">
                  <div class="file-icon"></div>
                  <div class="file-info">
                    <div class="file-name">{{ message.fileName }}</div>
                    <div class="file-size">{{ formatFileSize(message.fileSize) }}</div>
                  </div>
                  <div class="download-icon"></div>
                </div>
                
                <div v-else-if="message.messageType === 'emoji'" class="message-emoji">
                  {{ message.content }}
                </div>
              </div>
              <!-- 原自己的头像逻辑已合并到上面的统一头像显示区域 -->
            </div>
          </div>
          
          <!-- 新消息提示 -->
          <div v-if="hasNewMessage && !isAtBottom" class="new-message-indicator" @click="scrollToBottom">
            <i class="arrow-down-icon"></i>
            <span>有新消息</span>
          </div>
        </div>
      </div>
      
      <!-- 用户列表侧边栏 -->
      <div class="sidebar">
        <div class="sidebar-content">
          <!-- 在线用户部分 -->
          <div class="sidebar-section users-section">
            <div class="sidebar-header">
              <h2>在线用户 ({{ onlineUsers.length }})</h2>
            </div>
            <div class="user-list">
              <div v-for="user in onlineUsers" :key="user.id" class="user-item" @click="showUserCard(user.id)">
                <div class="user-avatar" :class="{ 'vip-avatar': user.memberLevel === 1, 'svip-avatar': user.memberLevel === 2 }">
                  <img v-if="user.avatarUrl" :src="processAvatarUrl(user.avatarUrl)" alt="用户头像" class="avatar-image" />
                  <div v-else class="default-avatar">{{ user.username.charAt(0).toUpperCase() }}</div>
                </div>
                <div class="user-details">
                  <div class="user-name">
                    {{ user.username }}
                    <span v-if="user.memberLevel === 1" class="vip-tag">VIP</span>
                    <span v-else-if="user.memberLevel === 2" class="svip-tag">SVIP</span>
                  </div>
                  <div class="user-status" :class="user.status">{{ user.status === 'online' ? '在线' : '离线' }}</div>
                </div>
              </div>
            </div>
          </div>
          
          <!-- AI总结部分 -->
          <div class="sidebar-section summary-section">
            <div class="sidebar-header">
              <h2>AI实时总结</h2>
              <div class="summary-status" :class="{ 'active': autoSummaryActive }">
                {{ summarizing ? '正在总结...' : autoSummaryActive ? '自动总结已开启' : '自动总结已暂停' }}
              </div>
            </div>
            <div class="summary-content-container">
              <div v-if="summaryError" class="summary-error">
                <p>{{ summaryError }}</p>
              </div>
              <div v-else-if="summarizing && !chatSummary" class="summary-loading">
                <div class="loading-spinner"></div>
                <p>AI正在分析聊天记录...</p>
              </div>
              <div v-else-if="!chatSummary" class="summary-empty">
                <i class="summary-icon"></i>
                <p>暂无总结内容</p>
                <p class="summary-hint">将自动分析最近的聊天内容</p>
              </div>
              <div v-else class="auto-summary-content" v-html="formattedSummary"></div>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- 底部输入区 -->
    <footer class="chat-footer">
      <!-- 工具栏 -->
      <div class="toolbar">
        <div class="tool-button emoji-button" @click="toggleEmojiPanel">
          <i class="emoji-icon"></i>
        </div>
        <div class="tool-button image-button" @click.stop.prevent="triggerImageUpload">
          <input type="file" accept="image/*" ref="imageInput" @change="handleImageUpload" class="file-input" />
          <i class="image-icon"></i>
        </div>
        <div class="tool-button file-button" @click.stop.prevent="triggerFileUpload">
          <input type="file" ref="fileInput" @change="handleFileUpload" class="file-input" />
          <i class="file-icon"></i>
        </div>
      </div>
      
      <!-- 输入框 -->
      <div class="input-container">
        <!-- 表情面板 -->
        <div v-if="showEmojiPanel" class="emoji-panel">
          <div v-for="emoji in emojis" :key="emoji" class="emoji-item" @click="insertEmoji(emoji)">
            {{ emoji }}
          </div>
        </div>
        
        <textarea 
          class="message-input" 
          v-model="messageText" 
          placeholder="输入消息..." 
          @keydown.enter.prevent="sendMessage"
          ref="messageInput"></textarea>
      </div>
      
      <!-- 发送按钮 -->
      <button class="send-button" @click="sendMessage" :disabled="!isConnected || !messageText.trim()">
        <i class="send-icon"></i>
        <span>发送</span>
      </button>
    </footer>

    <!-- 图片预览弹窗 -->
    <div v-if="previewImageUrl" class="image-preview-modal" @click="closeImagePreview">
      <div class="image-preview-content">
        <img 
          :src="getFullImageUrl(previewImageUrl)" 
          alt="图片预览" 
          @error="handleImageError" />
        <button class="close-preview" @click.stop="closeImagePreview">×</button>
      </div>
    </div>

    <!-- 用户名片弹窗 -->
    <user-card 
      v-if="showingUserCard" 
      :visible="showingUserCard"
      :user-id="selectedUserId"
      :is-friend="isUserFriend(selectedUserId)"
      @close="closeUserCard"
      @friend-request-sent="handleFriendRequestSent"
    />

    <!-- 提示信息 -->
    <div v-if="notification.show" class="notification" :class="notification.type">
      {{ notification.message }}
    </div>
  </div>
</template>

<script>
import { ref, onMounted, onBeforeUnmount, computed, nextTick, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';
import UserCard from '@/components/UserCard.vue';

export default {
  name: 'ChatRoom',
  components: {
    UserCard
  },
  props: {
    id: {
      type: String,
      required: true
    }
  },
  setup(props) {
    const route = useRoute();
    const router = useRouter();
    
    // 用户信息
    const userId = ref(parseInt(localStorage.getItem('userId') || '0'));
    const username = ref(localStorage.getItem('username') || '访客');
    const userAvatar = ref(localStorage.getItem('userAvatar') || '');
    if (userAvatar.value && !userAvatar.value.startsWith('http')) {
      userAvatar.value = userAvatar.value.startsWith('/') ? userAvatar.value : `/${userAvatar.value}`;
      userAvatar.value = `${window.apiBaseUrl}${userAvatar.value}`;
    }
    
    // 处理图片URL
    const getFullImageUrl = (url) => {
      if (!url) return null;
      
      // 如果已经是完整URL，直接返回
      if (url.startsWith('http')) return url;
      
      // 检查是否是OSS路径
      if (url.includes('aliyuncs.com')) return url;
      
      // 如果包含temp/uploads，可能是图片已迁移到其他位置
      if (url.includes('temp/uploads')) {
        // 尝试使用新路径格式
        return `${window.apiBaseUrl}/api/file/get?path=${encodeURIComponent(url)}`;
      }
      
      // 默认处理相对路径
      return `${window.apiBaseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
    };
    
    // 添加图片加载错误处理
    const handleImageError = (event) => {
      console.warn('图片加载失败:', event.target.src);
      
      const originalSrc = event.target.src;
      // 如果当前路径是/temp/路径但返回404，尝试替换为其他路径格式
      if (originalSrc.includes('/temp/')) {
        // 尝试提取文件名
        const fileName = originalSrc.split('/').pop();
        if (fileName) {
          // 尝试使用备用路径
          event.target.src = `${window.apiBaseUrl}/api/file/image/${fileName}`;
          console.log('尝试备用图片路径:', event.target.src);
          return;
        }
      }
      
      // 如果所有尝试都失败，使用内联SVG数据URL作为默认图片
      event.target.src = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTAwIiBoZWlnaHQ9IjEwMCIgdmlld0JveD0iMCAwIDEwMCAxMDAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PHJlY3QgeD0iMCIgeT0iMCIgd2lkdGg9IjEwMCIgaGVpZ2h0PSIxMDAiIGZpbGw9IiNmMmYyZjIiLz48cGF0aCBkPSJNNTAgMzBDNDIuMjY4IDMwIDM2IDM2LjI2OCAzNiA0NEM1NS40NjQgNDQgNjQgNTIuNTM2IDY0IDcyQzcxLjczMiA3MiA3OCA2NS43MzIgNzggNThDNzggNDIuNTM2IDY1LjQ2NCAzMCA1MCAzMFoiIGZpbGw9IiNlMWUxZTEiLz48Y2lyY2xlIGN4PSI1MCIgY3k9IjUwIiByPSIyMCIgZmlsbD0ibm9uZSIgc3Ryb2tlPSIjYmJiIiBzdHJva2Utd2lkdGg9IjQiIHN0cm9rZS1kYXNoYXJyYXk9IjUgNSIvPjx0ZXh0IHg9IjUwIiB5PSI1NSIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZmlsbD0iIzk5OSIgZm9udC1mYW1pbHk9IkFyaWFsLCBzYW5zLXNlcmlmIiBmb250LXNpemU9IjEyIj7lm77niYfpl6/orqE8L3RleHQ+PC9zdmc+'; 
      event.target.classList.add('image-load-error');
      event.target.style.maxWidth = '100px';
      event.target.style.maxHeight = '100px';
      event.target.alt = '图片加载失败';
    };
    // 聊天室信息
    const roomId = computed(() => parseInt(props.id));
    const roomName = ref('公共聊天室');
    
    // 连接状态
    const isConnected = ref(false);
    const connectionStatus = ref('正在连接...');
    const connection = ref(null);
    
    // 消息相关
    const messages = ref([]);
    const messageText = ref('');
    const loadingHistory = ref(true);
    const messagesContainer = ref(null);
    const messageInput = ref(null);
    const hasNewMessage = ref(false);
    const isAtBottom = ref(true);
    
    // 在线用户
    const onlineUsers = ref([]);
    
    // 用户头像缓存，用于提高性能
    const userAvatarCache = ref({});
    
    // 表情相关
    const showEmojiPanel = ref(false);
    const emojis = ref(['😀', '😃', '😄', '😁', '😆', '😅', '😂', '🤣', '🥲', '☺️', '😊', '😇', 
                      '🙂', '🙃', '😉', '😌', '😍', '🥰', '😘', '😗', '😙', '😚', '😋', '😛', 
                      '😝', '😜', '🤪', '🤨', '🧐', '🤓', '😎', '🥸', '🤩', '🥳', '😏', '😒']);
    
    // 文件上传相关
    const imageInput = ref(null);
    const fileInput = ref(null);
    
    // 图片预览
    const previewImageUrl = ref(null);
    
    // 通知提示
    const notification = ref({
      show: false,
      message: '',
      type: 'info',
      timeout: null
    });
    
    // 聊天总结相关
    const summarizing = ref(false);
    const chatSummary = ref('');
    const summaryError = ref('');
    const showSummaryModal = ref(false);
    
    // 用户名片相关
    const showingUserCard = ref(false);
    const selectedUserId = ref(null);
    const friendsList = ref([]);
    
    // 加载好友列表
    const loadFriendsList = async () => {
      try {
        const response = await axios.get(`${window.apiBaseUrl}/api/group/user/${userId.value}/private`);
        if (response.data && response.data.code === 200) {
          friendsList.value = response.data.data.map(group => {
            // 查找对方用户ID（好友ID）
            const otherMember = group.members?.find(m => m.id !== userId.value);
            return otherMember ? otherMember.id : null;
          }).filter(id => id !== null);
        }
      } catch (error) {
        console.error('获取好友列表失败:', error);
      }
    };
    
    // 判断用户是否是好友
    const isUserFriend = (userId) => {
      return friendsList.value.includes(userId);
    };
    
    // 显示用户名片
    const showUserCard = (userId) => {
      // 不要显示自己的名片
      if (userId === parseInt(localStorage.getItem('userId'))) {
        return;
      }
      
      selectedUserId.value = userId;
      showingUserCard.value = true;
    };
    
    // 关闭用户名片
    const closeUserCard = () => {
      showingUserCard.value = false;
      selectedUserId.value = null;
    };
    
    // 处理好友请求发送后的回调
    const handleFriendRequestSent = () => {
      closeUserCard();
      showNotification('好友请求已发送', 'success');
    };
    
    // 自动总结相关
    const autoSummaryActive = ref(true);
    const summaryEnabled = ref(true); // 是否启用总结功能
    const lastMessageTime = ref(Date.now());
    const autoSummaryInterval = ref(null);
    const summaryDebounceTimeout = ref(null);
    const lastSummaryTime = ref(0);
    const messageCountSinceLastSummary = ref(0);
    
    // 处理头像URL
    const processAvatarUrl = (avatarUrl) => {
      if (!avatarUrl) return null;
      
      // 如果是阿里云OSS的URL，直接返回
      if (avatarUrl.includes('aliyuncs.com')) {
        return avatarUrl;
      }
      
      // 如果已经是完整的http URL，直接返回
      if (avatarUrl.startsWith('http')) {
        return avatarUrl;
      }
      
      // 处理相对路径
      avatarUrl = avatarUrl.startsWith('/') ? avatarUrl : `/${avatarUrl}`;
      avatarUrl = `${window.apiBaseUrl}${avatarUrl}`;
      
      return avatarUrl;
    };
    
    // 获取用户头像
    const getUserAvatar = (userId) => {
      // 如果是当前用户，直接返回userAvatar
      if (userId === parseInt(localStorage.getItem('userId'))) {
        return userAvatar.value;
      }
      
      // 如果缓存中有，直接返回（即使是null也返回，防止重复请求）
      if (userId in userAvatarCache.value) {
        return userAvatarCache.value[userId];
      }
      
      // 从在线用户列表中查找
      const user = onlineUsers.value.find(user => user.id === userId);
      if (user && user.avatarUrl) {
        const processedUrl = processAvatarUrl(user.avatarUrl);
        userAvatarCache.value[userId] = processedUrl;
        return processedUrl;
      }
      
      // 如果找不到，且没有正在获取，尝试从服务器获取
      if (!userAvatarCache.value[`loading_${userId}`]) {
        // 设置初始值为null，防止重复请求
        userAvatarCache.value[userId] = null;
        // 使用setTimeout将请求放入下一个事件循环，避免在渲染期间触发请求
        setTimeout(() => {
          fetchUserAvatar(userId);
        }, 0);
      }
      return null;
    };
    
    // 从服务器获取用户头像
    const fetchUserAvatar = async (userId) => {
      // 防止重复请求，添加标记
      if (userAvatarCache.value[`loading_${userId}`]) {
        return;
      }
      
      try {
        // 设置加载标记
        userAvatarCache.value[`loading_${userId}`] = true;
        
        const response = await axios.get(`${window.apiBaseUrl}/api/user/${userId}`);
        if (response.data && response.data.code === 200) {
          const userData = response.data.data;
          if (userData && userData.avatar) {
            // 使用avatarUrl字段或avatar字段
            const avatarUrl = userData.avatarUrl || userData.avatar;
            const processedUrl = processAvatarUrl(avatarUrl);
            userAvatarCache.value[userId] = processedUrl;
            
            // 强制更新组件
            nextTick(() => {
              const index = onlineUsers.value.findIndex(user => user.id === userId);
              if (index !== -1) {
                // 更新在线用户的头像URL和会员等级
                onlineUsers.value[index] = { 
                  ...onlineUsers.value[index],
                  avatarUrl: processedUrl,
                  memberLevel: userData.memberLevel || 0
                };
              }
            });
            
            console.log(`用户 ${userId} 的头像已更新为: ${processedUrl}`);
          } else {
            // 即使没有头像也存储null，避免重复请求
            userAvatarCache.value[userId] = null;
          }
        } else {
          // 响应异常也缓存，避免重复请求
          userAvatarCache.value[userId] = null;
        }
      } catch (error) {
        console.error('获取用户头像失败:', error);
        // 错误情况下也设置为null，防止持续请求
        userAvatarCache.value[userId] = null;
      } finally {
        // 清除加载标记
        userAvatarCache.value[`loading_${userId}`] = false;
      }
    };
    
    // 创建SignalR连接
    const createConnection = () => {
      // 获取正确的API基础URL
      let apiBaseUrl = window.apiBaseUrl || 'http://localhost:5067';
      
      // 调试输出当前使用的API基础URL
      console.log('使用API基础URL:', apiBaseUrl);
      
      // 创建新的连接
      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(`${apiBaseUrl}/chatHub`)
        .withAutomaticReconnect([0, 2000, 10000, 30000]) // 重连策略
        .configureLogging(signalR.LogLevel.Information)
        .build();
      
      // 确保全局API基础URL正确设置
      window.apiBaseUrl = apiBaseUrl;
      console.log('设置window.apiBaseUrl为:', window.apiBaseUrl);
      
      // 连接状态变化监听
      connection.value.onreconnecting(() => {
        isConnected.value = false;
        connectionStatus.value = '正在重新连接...';
        showNotification('连接断开，正在尝试重连...', 'warning');
      });
      
      connection.value.onreconnected(() => {
        isConnected.value = true;
        connectionStatus.value = '已连接';
        showNotification('已重新连接到聊天室', 'success');
        
        // 重新加入聊天室
        joinChatRoom();
      });
      
      connection.value.onclose(() => {
        isConnected.value = false;
        connectionStatus.value = '连接已关闭';
        showNotification('连接已关闭', 'error');
      });
      
      // 注册接收消息的处理函数
      registerSignalRHandlers();
      
      // 启动连接
      startConnection();
    };
    
    // 加载聊天室信息
    const loadRoomInfo = async () => {
      try {
        const response = await axios.get(`${window.apiBaseUrl}/api/chat/room/${roomId.value}`);
        if (response.data && response.data.code === 200) {
          const roomData = response.data.data;
          roomName.value = roomData.roomName;
          if (roomData.activeUserCount > 0 && onlineUsers.value.length === 0) {
            // 如果数据库中有在线用户但当前列表为空，刷新在线用户列表
            await loadRoomOnlineUsers();
          }
        } else {
          console.error('获取聊天室信息失败:', response.data?.msg || '未知错误');
        }
      } catch (error) {
        console.error('加载聊天室信息失败:', error);
      }
    };
    
    // 加载聊天室在线用户
    const loadRoomOnlineUsers = async () => {
      try {
        const response = await axios.get(`${window.apiBaseUrl}/api/chat/room/${roomId.value}/users`);
        if (response.data && response.data.code === 200) {
          // 更新在线用户列表
          onlineUsers.value = response.data.data;
          
          // 异步加载用户头像和会员等级
          onlineUsers.value.forEach(user => {
            if (!user.avatarUrl || user.memberLevel === undefined) {
              fetchUserAvatar(user.id);
            }
          });
        } else {
          console.error('获取聊天室在线用户失败:', response.data?.msg || '未知错误');
        }
      } catch (error) {
        console.error('加载聊天室在线用户失败:', error);
      }
    };
    
    // 注册SignalR处理函数
    const registerSignalRHandlers = () => {
      // 接收聊天消息
      connection.value.on('ReceiveMessage', (message) => {
        console.log('收到消息:', message);
        
        // 如果是图片消息，处理图片URL
        if (message.messageType === 'image' && message.fileUrl) {
          message.fileUrl = getFullImageUrl(message.fileUrl);
        }
        
        // 批量更新消息，减少重渲染次数
        if (messageUpdateTimeout) {
          clearTimeout(messageUpdateTimeout);
          pendingMessages.push(message);
        } else {
          pendingMessages.push(message);
          messageUpdateTimeout = setTimeout(() => {
            // 一次性添加所有待处理消息
            messages.value = [...messages.value, ...pendingMessages];
            pendingMessages = [];
            messageUpdateTimeout = null;
            
            // 检查是否需要滚动到底部
            if (isAtBottom.value) {
              nextTick(() => scrollToBottom());
            } else {
              hasNewMessage.value = true;
            }
          }, 100); // 100ms批处理窗口
        }
        
        // 如果是其他用户发送的消息，且不是系统消息，显示消息通知
        if (message.senderId !== userId.value && message.messageType !== 'system') {
          showNotification(`${message.senderName}: ${message.messageType === 'text' ? message.content : '[' + message.messageType + '消息]'}`, 'info');
        }
        
        // 记录消息活动，用于自动总结功能
        recordMessageActivity();
      });
      
      // 消息批处理变量
      let pendingMessages = [];
      let messageUpdateTimeout = null;
      
      // 接收历史消息
      connection.value.on('ReceiveHistoryMessages', (historyMessages) => {
        console.log('收到历史消息:', historyMessages);
        loadingHistory.value = false;
        
        if (historyMessages && historyMessages.length > 0) {
          // 处理历史消息中的图片URL
          const processedMessages = historyMessages.map(msg => ({
            ...msg,
            fileUrl: msg.fileUrl ? getFullImageUrl(msg.fileUrl) : null
          }));
          
          // 将历史消息添加到消息列表
          messages.value = processedMessages.sort((a, b) => 
            new Date(a.sendTime) - new Date(b.sendTime)
          );
          
          // 滚动到底部
          nextTick(() => scrollToBottom());
          
          // 初始请求一次总结
          if (historyMessages.length >= 5) {
            setTimeout(() => {
              requestChatSummary(true);
            }, 1000);
          }
        }
      });
      
      // 更新在线用户列表
      connection.value.on('UpdateOnlineUsers', async (users) => {
        console.log('更新在线用户列表:', users);
        
        // 使用服务器推送的用户列表更新，但从数据库获取补充信息
        if (users && users.length > 0) {
          onlineUsers.value = users;
          
          // 更新用户头像和会员等级（异步）
          for (const user of users) {
            if (!user.avatarUrl || user.memberLevel === undefined) {
              fetchUserAvatar(user.id);
            }
          }
        } else {
          // 如果为空，尝试从API获取用户列表
          await loadRoomOnlineUsers();
        }
      });
      
      // 接收错误消息
      connection.value.on('Error', (errorMessage) => {
        console.error('SignalR错误:', errorMessage);
        showNotification(errorMessage, 'error');
      });
    };
    
    // 启动连接
    const startConnection = async () => {
      try {
        connectionStatus.value = '正在连接...';
        await connection.value.start();
        isConnected.value = true;
        connectionStatus.value = '已连接';
        console.log('SignalR连接已建立');
        
        // 连接成功后加入聊天室
        joinChatRoom();
        
        // 加载聊天室信息
        await loadRoomInfo();
        
        // 启动定时刷新功能
        startAutoRefreshMessages();
      } catch (error) {
        console.error('连接SignalR失败:', error);
        connectionStatus.value = '连接失败';
        isConnected.value = false;
        
        // 5秒后重试
        setTimeout(startConnection, 5000);
      }
    };
    
    // 加入聊天室
    const joinChatRoom = async () => {
      if (!isConnected.value) {
        showNotification('尚未连接到服务器', 'error');
        return;
      }
      
      try {
        // 调用后端方法加入聊天室
        await connection.value.invoke('JoinChatRoom', userId.value, username.value, roomId.value);
        console.log(`成功加入聊天室 ${roomId.value}`);
      } catch (error) {
        console.error('加入聊天室失败:', error);
        showNotification('加入聊天室失败: ' + error, 'error');
      }
    };
    
    // 发送文本消息
    const sendMessage = async () => {
      if (!isConnected.value) {
        showNotification('未连接到服务器，无法发送消息', 'error');
        return;
      }
      
      if (!messageText.value.trim()) {
        return;
      }
      
      try {
        // 发送消息
        await connection.value.invoke('SendMessageToRoom', messageText.value);
        console.log('消息已发送');
        
        // 清空输入框
        messageText.value = '';
        
        // 让输入框重新获取焦点
        messageInput.value.focus();
      } catch (error) {
        console.error('发送消息失败:', error);
        showNotification('发送消息失败: ' + error, 'error');
      }
    };
    
    // 修改表情点击函数
    const insertEmoji = (emoji) => {
      // 将表情添加到输入框，而不是直接发送
      messageText.value += emoji;
      // 聚焦输入框
      nextTick(() => {
        messageInput.value.focus();
      });
      // 关闭表情面板
      showEmojiPanel.value = false;
    };
    
    // 修改上传图片函数
    const handleImageUpload = async (event) => {
      if (!event.target.files || event.target.files.length === 0) {
        return;
      }
      
      // 禁用上传按钮，防止重复上传
      const imageButton = document.querySelector('.image-button');
      if (imageButton) imageButton.classList.add('disabled');
      
      const file = event.target.files[0];
      
      // 检查文件类型
      const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/bmp', 'image/webp'];
      if (!allowedTypes.includes(file.type)) {
        showNotification('只支持jpg、jpeg、png、gif、bmp、webp格式的图片', 'error');
        if (imageButton) imageButton.classList.remove('disabled');
        return;
      }
      
      // 检查文件大小（10MB）
      const maxSize = 10 * 1024 * 1024;
      if (file.size > maxSize) {
        showNotification('图片大小不能超过10MB', 'error');
        if (imageButton) imageButton.classList.remove('disabled');
        return;
      }
      
      const formData = new FormData();
      formData.append('file', file);
      
      try {
        // 显示上传中提示
        showNotification('图片上传中...', 'info');
        
        console.log('开始上传图片到:', `${window.apiBaseUrl}/api/file/upload`);
        
        // 使用统一的API基础URL
        const response = await axios.post(`${window.apiBaseUrl}/api/file/upload`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
        console.log('图片上传成功:', response.data);
        
        // 发送图片消息
        await connection.value.invoke(
          'SendImageToRoom', 
          response.data.url, 
          response.data.fileName, 
          response.data.fileSize
        );
        
        // 清空文件输入框，允许重复上传相同文件
        imageInput.value.value = '';
        
        showNotification('图片发送成功', 'success');
      } catch (error) {
        console.error('图片上传失败:', error);
        // 更详细的错误日志
        if (error.response) {
          console.error('错误状态:', error.response.status);
          console.error('错误数据:', error.response.data);
          console.error('错误头部:', error.response.headers);
        }
        showNotification('图片上传失败: ' + (error.response?.data?.message || error.response?.data || error.message), 'error');
        
        // 清空文件输入框
        imageInput.value.value = '';
      } finally {
        // 恢复上传按钮状态
        if (imageButton) imageButton.classList.remove('disabled');
      }
    };
    
    // 防抖变量
    let uploadClickTimeout = null;
    
    // 添加触发图片上传点击的辅助方法
    const triggerImageUpload = () => {
      if (!isConnected.value) {
        showNotification('未连接到服务器，无法发送图片', 'error');
        return;
      }
      
      // 防止重复触发
      if (uploadClickTimeout) {
        clearTimeout(uploadClickTimeout);
      }
      
      uploadClickTimeout = setTimeout(() => {
        uploadClickTimeout = null;
        
        // 确保输入框引用存在并处于可用状态
        if (imageInput.value) {
          // 直接设置value为空（重置），确保相同文件可以被再次选中
          imageInput.value.value = '';
          
          try {
            // 使用MouseEvent构造函数创建原生点击事件
            const clickEvent = new MouseEvent('click', {
              bubbles: false,  // 不冒泡
              cancelable: true,
              view: window
            });
            
            // 直接在input元素上触发点击事件
            imageInput.value.dispatchEvent(clickEvent);
          } catch (error) {
            console.error('触发文件选择器失败:', error);
            // 降级方案
            imageInput.value.click();
          }
        } else {
          console.warn('找不到图片输入框引用');
          showNotification('上传组件未就绪，请刷新页面重试', 'error');
        }
      }, 100); // 降低延迟时间
    };
    
    // 添加触发文件上传点击的辅助方法
    const triggerFileUpload = () => {
      if (!isConnected.value) {
        showNotification('未连接到服务器，无法发送文件', 'error');
        return;
      }
      
      // 防止重复触发
      if (uploadClickTimeout) {
        clearTimeout(uploadClickTimeout);
      }
      
      uploadClickTimeout = setTimeout(() => {
        uploadClickTimeout = null;
        
        // 确保输入框引用存在并处于可用状态
        if (fileInput.value) {
          // 直接设置value为空（重置），确保相同文件可以被再次选中
          fileInput.value.value = '';
          
          try {
            // 使用MouseEvent构造函数创建原生点击事件
            const clickEvent = new MouseEvent('click', {
              bubbles: false,  // 不冒泡
              cancelable: true,
              view: window
            });
            
            // 直接在input元素上触发点击事件
            fileInput.value.dispatchEvent(clickEvent);
          } catch (error) {
            console.error('触发文件选择器失败:', error);
            // 降级方案
            fileInput.value.click();
          }
        } else {
          console.warn('找不到文件输入框引用');
          showNotification('上传组件未就绪，请刷新页面重试', 'error');
        }
      }, 100); // 降低延迟时间
    };
    
    // 修改上传文件函数
    const handleFileUpload = async (event) => {
      if (!event.target.files || event.target.files.length === 0) {
        return;
      }
      
      // 禁用上传按钮，防止重复上传
      const fileButton = document.querySelector('.file-button');
      if (fileButton) fileButton.classList.add('disabled');
      
      const file = event.target.files[0];
      const formData = new FormData();
      formData.append('file', file);
      
      try {
        // 显示上传中提示
        showNotification('文件上传中...', 'info');
        
        // 使用统一的API基础URL
        const response = await axios.post(`${window.apiBaseUrl}/api/file/upload`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
        console.log('文件上传成功:', response.data);
        
        // 发送文件消息
        await connection.value.invoke(
          'SendFileToRoom', 
          response.data.url, 
          response.data.fileName, 
          response.data.fileSize
        );
        
        // 清空文件输入框，允许重复上传相同文件
        fileInput.value.value = '';
        
        showNotification('文件发送成功', 'success');
      } catch (error) {
        console.error('文件上传失败:', error);
        // 更详细的错误日志
        if (error.response) {
          console.error('错误状态:', error.response.status);
          console.error('错误数据:', error.response.data);
          console.error('错误头部:', error.response.headers);
        }
        showNotification('文件上传失败: ' + (error.response?.data?.message || error.response?.data || error.message), 'error');
        
        // 清空文件输入框
        fileInput.value.value = '';
      } finally {
        // 恢复上传按钮状态
        if (fileButton) fileButton.classList.remove('disabled');
      }
    };
    
    // 切换表情面板
    const toggleEmojiPanel = () => {
      showEmojiPanel.value = !showEmojiPanel.value;
    };
    
    // 点击外部区域关闭表情面板
    const handleClickOutside = (event) => {
      const emojiButton = document.querySelector('.emoji-button');
      const emojiPanel = document.querySelector('.emoji-panel');
      
      if (emojiButton && emojiPanel && 
          !emojiButton.contains(event.target) && 
          !emojiPanel.contains(event.target)) {
        showEmojiPanel.value = false;
      }
    };
    
    // 图片预览
    const previewImage = (url) => {
      previewImageUrl.value = url;
    };
    
    // 关闭图片预览
    const closeImagePreview = () => {
      previewImageUrl.value = null;
    };
    
    // 下载文件
    const downloadFile = (url, fileName) => {
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      link.target = '_blank';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    };
    
    // 滚动到底部
    const scrollToBottom = () => {
      if (messagesContainer.value) {
        // 使用RAF确保在下一帧执行滚动，减少闪烁
        requestAnimationFrame(() => {
          messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
          hasNewMessage.value = false;
        });
      }
    };
    
    // 检查滚动位置
    const checkScrollPosition = () => {
      if (messagesContainer.value) {
        const { scrollTop, scrollHeight, clientHeight } = messagesContainer.value;
        // 使用防抖，减少状态更新频率
        if (scrollPositionDebounce) clearTimeout(scrollPositionDebounce);
        scrollPositionDebounce = setTimeout(() => {
          isAtBottom.value = Math.abs(scrollHeight - scrollTop - clientHeight) < 50;
        }, 100);
      }
    };
    
    // 退出聊天室
    const leaveRoom = () => {
      if (connection.value) {
        // 先通知服务器用户离开聊天室
        try {
          // 发送一个离开聊天室的消息
          const leaveMessage = {
            roomId: roomId.value,
            senderId: 0,
            senderName: "系统",
            content: `${username.value} 离开了聊天室`,
            messageType: "system"
          };
          
          // 执行离开聊天室的逻辑
          connection.value.invoke("LeaveRoom", roomId.value)
            .catch(err => console.error("离开聊天室失败:", err))
            .finally(() => {
              // 停止连接
              connection.value.stop();
              
              // 导航到主页
              router.push('/home');
            });
        } catch (error) {
          console.error("离开聊天室时出错:", error);
          connection.value.stop();
          router.push('/home');
        }
      } else {
        router.push('/home');
      }
    };
    
    // 显示通知
    const showNotification = (message, type = 'info') => {
      // 清除之前的定时器
      if (notification.value.timeout) {
        clearTimeout(notification.value.timeout);
      }
      
      // 设置新的通知
      notification.value = {
        show: true,
        message,
        type,
        timeout: setTimeout(() => {
          notification.value.show = false;
        }, 3000)
      };
    };
    
    // 格式化时间
    const formatTime = (dateString) => {
      const date = new Date(dateString);
      return date.toLocaleTimeString('zh-CN', {
        hour: '2-digit',
        minute: '2-digit'
      });
    };
    
    // 格式化日期
    const formatDate = (dateString) => {
      const date = new Date(dateString);
      const today = new Date();
      const yesterday = new Date(today);
      yesterday.setDate(yesterday.getDate() - 1);
      
      if (date.toDateString() === today.toDateString()) {
        return '今天';
      } else if (date.toDateString() === yesterday.toDateString()) {
        return '昨天';
      } else {
        return date.toLocaleDateString('zh-CN', {
          year: 'numeric',
          month: '2-digit',
          day: '2-digit'
        });
      }
    };
    
    // 格式化文件大小
    const formatFileSize = (bytes) => {
      if (!bytes) return '未知大小';
      
      const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
      if (bytes === 0) return '0 B';
      
      const i = Math.floor(Math.log(bytes) / Math.log(1024));
      return (bytes / Math.pow(1024, i)).toFixed(2) + ' ' + sizes[i];
    };
    
    // 获取消息类名
    const getMessageClass = (message) => {
      const classes = [];
      
      if (message.messageType === 'system') {
        classes.push('system-message-container');
      } else {
        classes.push(message.senderId === userId.value ? 'self-message-container' : 'other-message-container');
      }
      
      return classes.join(' ');
    };
    
    // 判断是否显示日期分隔线
    const shouldShowDateSeparator = (index) => {
      if (index === 0) return true;
      
      const currentDate = new Date(messages.value[index].sendTime).setHours(0, 0, 0, 0);
      const prevDate = new Date(messages.value[index - 1].sendTime).setHours(0, 0, 0, 0);
      
      return currentDate !== prevDate;
    };
    
    // 请求聊天总结
    const requestChatSummary = async (silent = false) => {
      if (!isConnected.value) {
        if (!silent) {
          showNotification('未连接到服务器，无法生成总结', 'error');
        }
        return;
      }
      
      // 检查自上次总结以来的时间
      const now = Date.now();
      if (now - lastSummaryTime.value < 10000 && silent) { // 10秒内不重复自动总结
        return;
      }
      
      // 如果是静默模式（自动总结）且消息计数器少于3条，则不总结
      if (silent && messageCountSinceLastSummary.value < 3) {
        return;
      }
      
      summarizing.value = true;
      summaryError.value = '';
      
      try {
        const response = await axios.post(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/summarize`, {
          roomId: roomId.value,
          userId: userId.value,
          username: username.value,
          messageCount: 100 // 默认获取最近100条消息
        });
        
        if (response.data && response.data.success) {
          chatSummary.value = response.data.message;
          lastSummaryTime.value = now;
          messageCountSinceLastSummary.value = 0;
        } else {
          summaryError.value = response.data?.error || '生成总结失败，请稍后重试';
          if (!silent) {
            showNotification('总结生成失败: ' + summaryError.value, 'error');
          }
        }
      } catch (error) {
        console.error('获取聊天总结失败:', error);
        summaryError.value = '获取聊天总结失败: ' + (error.response?.data?.error || error.message);
        if (!silent) {
          showNotification('总结生成失败: ' + summaryError.value, 'error');
        }
      } finally {
        summarizing.value = false;
      }
    };
    
    // 自动执行聊天总结
    const setupAutoSummary = () => {
      // 每30秒检查是否需要更新总结
      autoSummaryInterval.value = setInterval(() => {
        if (!autoSummaryActive.value) return;
        
        const inactiveThreshold = 60000; // 1分钟无活动则不自动总结
        const now = Date.now();
        
        if (now - lastMessageTime.value > inactiveThreshold) {
          // 聊天不活跃，不进行总结
          console.log('聊天不活跃，跳过自动总结');
          return;
        }
        
        // 执行自动总结
        requestChatSummary(true);
      }, 30000); // 30秒
    };

    // 在收到新消息时记录时间和计数
    const recordMessageActivity = () => {
      lastMessageTime.value = Date.now();
      messageCountSinceLastSummary.value++;
      
      // 防抖处理，当快速收到多条消息时，等待一定时间后再总结
      if (summaryDebounceTimeout.value) {
        clearTimeout(summaryDebounceTimeout.value);
      }
      
      summaryDebounceTimeout.value = setTimeout(() => {
        // 如果收到至少5条新消息，自动触发总结
        if (messageCountSinceLastSummary.value >= 5) {
          requestChatSummary(true);
        }
      }, 5000); // 5秒后检查是否需要总结
    };
    
    // 检查是否需要更新总结
    const checkForSummaryUpdate = () => {
      const now = Date.now();
      // 如果距离上次总结已经过了5分钟，或者有至少10条新消息，则更新总结
      if ((now - lastSummaryTime.value > 300000 || messageCountSinceLastSummary.value >= 10) && 
          messages.value.length > 0) {
        requestChatSummary(true);
      }
    };
    
    // 格式化总结内容
    const formattedSummary = computed(() => {
      if (!chatSummary.value) return '';
      
      let formatted = chatSummary.value;
      
      // 处理标题
      if (formatted.includes('聊天记录总结')) {
        formatted = formatted.replace(/^聊天记录总结.*$/m, '<h2>聊天记录总结</h2>');
      } else {
        // 如果没有标题，添加一个默认标题
        formatted = '<h2>聊天记录总结</h2>' + formatted;
      }
      
      // 处理小标题
      const headings = ['主要话题', '重要观点和信息', '提出的问题', '达成的共识或结论', '补充观察'];
      headings.forEach(heading => {
        formatted = formatted.replace(new RegExp(`^${heading}$`, 'm'), `<h3>${heading}</h3>`);
      });
      
      // 处理列表项
      let sections = [];
      const parts = formatted.split('<h3>');
      
      // 处理第一部分（标题部分）
      if (parts[0]) {
        sections.push(parts[0]);
      }
      
      // 处理每个小标题部分
      for (let i = 1; i < parts.length; i++) {
        let part = parts[i];
        const headingEndIndex = part.indexOf('</h3>');
        
        if (headingEndIndex !== -1) {
          const heading = part.substring(0, headingEndIndex);
          let content = part.substring(headingEndIndex + 5);
          
          // 处理列表项
          content = content.replace(/^- (.*?)$/gm, '<li>$1</li>');
          
          // 如果有列表项，将它们包装在ul标签中
          if (content.includes('<li>')) {
            const listItems = content.match(/<li>.*?<\/li>/gs) || [];
            const nonListContent = content.split(/<li>.*?<\/li>/gs).filter(Boolean);
            
            let processedContent = '';
            let currentIndex = 0;
            
            // 重建内容，将列表项包装在ul标签中
            for (let j = 0; j < nonListContent.length; j++) {
              processedContent += nonListContent[j];
              
              let listGroup = '';
              while (currentIndex < listItems.length && 
                     content.indexOf(listItems[currentIndex], 
                     processedContent.length) === processedContent.length) {
                listGroup += listItems[currentIndex];
                currentIndex++;
              }
              
              if (listGroup) {
                processedContent += `<ul>${listGroup}</ul>`;
              }
            }
            
            // 处理剩余的列表项
            if (currentIndex < listItems.length) {
              processedContent += `<ul>${listItems.slice(currentIndex).join('')}</ul>`;
            }
            
            content = processedContent || `<ul>${content}</ul>`;
          }
          
          sections.push(`<h3>${heading}</h3>${content}`);
        } else {
          sections.push(`<h3>${part}</h3>`);
        }
      }
      
      formatted = sections.join('');
      
      // 处理Markdown风格的格式
      formatted = formatted.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
      formatted = formatted.replace(/\*(.*?)\*/g, '<em>$1</em>');
      
      // 处理换行符
      formatted = formatted.replace(/\n/g, '<br>');
      
      // 美化注释部分
      formatted = formatted.replace(/注：(.*?)(?:<br>|$)/g, '<div class="summary-note"><strong>注：</strong>$1</div>');
      
      return formatted;
    });
    
    // 组件挂载时
    onMounted(() => {
      roomId.value = parseInt(props.id);
      // 初始化连接
      createConnection();
      
      // 添加事件监听器
      document.addEventListener('click', handleClickOutside);
      messagesContainer.value?.addEventListener('scroll', checkScrollPosition);
      
      // 定时更新AI总结
      autoSummaryInterval.value = setInterval(() => {
        if (messages.value.length > 0 && summaryEnabled.value && !summarizing.value) {
          checkForSummaryUpdate();
        }
      }, 60000); // 每分钟检查一次
      
      // 如果没有实时连接，尝试直接从API获取聊天室信息
      if (!isConnected.value) {
        loadRoomInfo();
      }
    });
    
    // 组件卸载前
    onBeforeUnmount(() => {
      // 停止SignalR连接
      if (connection.value) {
        connection.value.stop();
      }
      
      // 移除滚动事件监听
      if (messagesContainer.value) {
        messagesContainer.value.removeEventListener('scroll', checkScrollPosition);
      }
      
      // 移除其他事件监听
      document.removeEventListener('click', () => {});
      
      // 清除定时器
      if (notification.value.timeout) {
        clearTimeout(notification.value.timeout);
      }
      
      // 清除自动总结相关的定时器
      if (autoSummaryInterval.value) {
        clearInterval(autoSummaryInterval.value);
      }
      
      if (summaryDebounceTimeout.value) {
        clearTimeout(summaryDebounceTimeout.value);
      }
      
      // 清除上传防抖超时
      if (uploadClickTimeout) {
        clearTimeout(uploadClickTimeout);
      }
      
      // 停止自动刷新
      stopAutoRefreshMessages();
    });
    
    // 监听消息列表变化
    watch(messages, () => {
      if (isAtBottom.value) {
        nextTick(() => scrollToBottom());
      }
    });
    
    // 使用防抖变量
    let scrollPositionDebounce = null;
    
    // 自动刷新消息定时器ID
    let autoRefreshTimerId = null;
    
    // 开始自动刷新消息和在线用户
    const startAutoRefreshMessages = () => {
      // 清除可能存在的旧定时器
      if (autoRefreshTimerId) {
        clearInterval(autoRefreshTimerId);
      }
      
      // 设置新的定时器，每10秒刷新一次
      autoRefreshTimerId = setInterval(async () => {
        if (isConnected.value) {
          // 获取最新的聊天室信息和在线用户
          await loadRoomInfo();
          // 查询最新消息（这里通过SignalR Hub直接请求）
          try {
            await connection.value.invoke('RequestLatestMessages', roomId.value, 20);
          } catch (error) {
            console.error('请求最新消息失败:', error);
          }
        }
      }, 3000); // 10秒刷新一次
    };
    
    // 停止自动刷新
    const stopAutoRefreshMessages = () => {
      if (autoRefreshTimerId) {
        clearInterval(autoRefreshTimerId);
        autoRefreshTimerId = null;
      }
    };
    
    return {
      // 用户信息
      userId,
      username,
      userAvatar,
      
      // 聊天室信息
      roomId,
      roomName,
      
      // 连接状态
      isConnected,
      connectionStatus,
      
      // 消息相关
      messages,
      messageText,
      loadingHistory,
      messagesContainer,
      messageInput,
      hasNewMessage,
      isAtBottom,
      
      // 在线用户
      onlineUsers,
      
      // 表情相关
      showEmojiPanel,
      emojis,
      
      // 文件上传相关
      imageInput,
      fileInput,
      
      // 图片预览
      previewImageUrl,
      
      // 通知
      notification,
      
      // 聊天总结相关
      summarizing,
      chatSummary,
      summaryError,
      autoSummaryActive,
      summaryEnabled,
      
      // 用户名片相关
      showingUserCard,
      selectedUserId,
      showUserCard,
      closeUserCard,
      isUserFriend,
      handleFriendRequestSent,
      
      // 头像处理
      processAvatarUrl,
      getUserAvatar,
      
      // 方法
      sendMessage,
      insertEmoji,
      handleImageUpload,
      handleFileUpload,
      toggleEmojiPanel,
      previewImage,
      closeImagePreview,
      downloadFile,
      scrollToBottom,
      leaveRoom,
      formatTime,
      formatDate,
      formatFileSize,
      getMessageClass,
      shouldShowDateSeparator,
      requestChatSummary,
      formattedSummary,
      getFullImageUrl,
      handleImageError,
      triggerImageUpload,
      triggerFileUpload,
      startAutoRefreshMessages,
      stopAutoRefreshMessages,
    };
  }
};
</script>

<style scoped>
/* 主容器 */
.chat-room-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f7fa;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
  color: #333;
  overflow: hidden;
  position: relative;
  /* 添加硬件加速 */
  transform: translateZ(0);
  will-change: transform;
}

/* 头部样式 */
.chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 20px;
  background-color: #fff;
  border-bottom: 1px solid #e5e5e5;
  height: 60px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.1);
  z-index: 10;
}

.room-info {
  display: flex;
  flex-direction: column;
}

.room-info h1 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.room-status {
  display: flex;
  align-items: center;
  margin-top: 5px;
  font-size: 12px;
  color: #666;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background-color: #ff4d4f;
  margin-right: 6px;
  transition: background-color 0.3s ease;
}

.status-indicator.connected {
  background-color: #52c41a;
}

.status-text {
  margin-right: 10px;
}

.online-count {
  font-weight: 500;
  color: #1677ff;
}

.user-info {
  display: flex;
  align-items: center;
}

.username {
  margin-right: 10px;
  font-weight: 500;
}

.avatar, .default-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  background: linear-gradient(135deg, #1677ff, #69c0ff);
  color: white;
  font-weight: bold;
  font-size: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}
.avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
}

.leave-button {
  padding: 6px 16px;
  background-color: #ff4d4f;
  color: white;
  border: none;
  border-radius: 18px;
  cursor: pointer;
  font-size: 14px;
  margin-left: 8px;
  transition: background-color 0.3s ease;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.leave-button:hover {
  background-color: #ff7875;
}

/* 主内容区 */
.chat-main {
  display: flex;
  flex: 1;
  overflow: hidden;
}

/* 消息容器 */
.messages-container {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
  position: relative;
  /* 添加平滑滚动效果 */
  scroll-behavior: smooth;
  -webkit-overflow-scrolling: touch;
  /* 减少闪烁 */
  backface-visibility: hidden;
}

.messages-wrapper {
  display: flex;
  flex-direction: column;
  min-height: 100%;
}

.loading-message {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 15px;
  color: #666;
  font-size: 14px;
}

.loading-spinner {
  width: 20px;
  height: 20px;
  border: 2px solid rgba(0, 0, 0, 0.1);
  border-top-color: #1677ff;
  border-radius: 50%;
  margin-right: 10px;
  animation: spinner 0.8s linear infinite;
}

@keyframes spinner {
  to {
    transform: rotate(360deg);
  }
}

/* 日期分隔线 */
.date-separator {
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 15px 0;
  color: #999;
  font-size: 12px;
}

.date-separator span {
  padding: 2px 10px;
  background-color: #f0f2f5;
  border-radius: 10px;
}

/* 消息样式 */
.message-item {
  display: flex;
  flex-direction: column;
  margin-bottom: 15px;
  /* 使用不那么激进的动画 */
  animation: fadeInSmooth 0.2s ease;
  /* 添加硬件加速 */
  transform: translateZ(0);
  will-change: transform, opacity;
}

@keyframes fadeInSmooth {
  from {
    opacity: 0.7;
    transform: translateY(5px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* 系统消息 */
.system-message-container {
  align-items: center;
}

.system-message {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.system-message-content {
  display: flex;
  align-items: center;
  background-color: rgba(0, 0, 0, 0.03);
  padding: 6px 12px;
  border-radius: 15px;
  font-size: 12px;
  color: #999;
}

.system-icon {
  display: inline-block;
  width: 14px;
  height: 14px;
  background-color: #dadada;
  border-radius: 50%;
  margin-right: 6px;
}

/* 用户消息 */
.user-message {
  display: flex;
  align-items: flex-end;
  gap: 10px;
}

.user-message.self-message {
  flex-direction: row-reverse;
}

.message-avatar, .self-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  overflow: hidden;
  background: #e6f7ff;
  display: flex;
  align-items: center;
  justify-content: center;
}

.message-avatar img, .self-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
}

.message-content {
  max-width: 400px;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
}

.user-message.self-message .message-content {
  align-items: flex-end;
  text-align: right;
}

/* 文本消息 */
.message-text {
  padding: 10px 15px;
  border-radius: 6px;
  font-size: 14px;
  word-break: break-word;
  line-height: 1.5;
  position: relative;
  background-color: #ffffff;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
}

.user-message:not(.self-message) .message-text {
  border-top-left-radius: 0;
}

.user-message.self-message .message-text {
  background-color: #1677ff;
  color: white;
  border-top-right-radius: 0;
}

.message-info {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
  font-size: 12px;
  color: #666;
}

.self-message .message-info {
  flex-direction: row-reverse;
}

.message-time {
  color: #999;
  font-size: 12px;
}

.self-message .message-time {
  text-align: right;
}

/* 图片消息 */
.message-image {
  max-width: 250px;
  overflow: hidden;
  border-radius: 6px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transition: transform 0.3s ease;
  cursor: pointer;
}

.message-image img {
  width: 100%;
  display: block;
  border-radius: 6px;
}

.message-image:hover {
  transform: scale(1.02);
}

.image-info {
  padding: 5px 10px;
  font-size: 12px;
  color: #999;
  background-color: rgba(255, 255, 255, 0.8);
  text-align: center;
}

/* 文件消息 */
.message-file {
  display: flex;
  align-items: center;
  background-color: #ffffff;
  padding: 10px;
  border-radius: 6px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.message-file:hover {
  background-color: #f5f7fa;
}

.file-icon {
  width: 32px;
  height: 40px;
  background-color: #e6f7ff;
  border-radius: 4px;
  margin-right: 10px;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
}

.file-icon::before {
  content: "";
  display: block;
  width: 15px;
  height: 15px;
  background-color: #1677ff;
  clip-path: polygon(0% 0%, 70% 0%, 100% 30%, 100% 100%, 0% 100%);
}

.file-info {
  flex: 1;
  min-width: 0;
}

.file-name {
  font-size: 14px;
  font-weight: 500;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.file-size {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}

.download-icon {
  width: 20px;
  height: 20px;
  margin-left: 10px;
  position: relative;
}

.download-icon::before {
  content: "";
  display: block;
  width: 12px;
  height: 12px;
  border: 2px solid #1677ff;
  border-top: none;
  border-left: none;
  transform: rotate(45deg);
  position: absolute;
  top: 0;
  left: 4px;
}

.download-icon::after {
  content: "";
  display: block;
  width: 2px;
  height: 14px;
  background-color: #1677ff;
  position: absolute;
  top: 0;
  left: 9px;
}

/* 表情消息 */
.message-emoji {
  font-size: 24px;
  padding: 10px;
  background-color: transparent;
}

/* 新消息提示 */
.new-message-indicator {
  position: fixed;
  bottom: 100px;
  left: 50%;
  transform: translateX(-50%);
  background-color: #1677ff;
  color: white;
  padding: 8px 16px;
  border-radius: 20px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  display: flex;
  align-items: center;
  cursor: pointer;
  /* 替换为更轻微的动画，减少闪烁 */
  animation: pulse 2s infinite;
  z-index: 5;
}

@keyframes pulse {
  0% {
    box-shadow: 0 0 0 0 rgba(22, 119, 255, 0.4);
  }
  70% {
    box-shadow: 0 0 0 10px rgba(22, 119, 255, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(22, 119, 255, 0);
  }
}

.arrow-down-icon {
  width: 0;
  height: 0;
  border-left: 6px solid transparent;
  border-right: 6px solid transparent;
  border-top: 6px solid white;
  margin-right: 8px;
}

/* 侧边栏 */
.sidebar {
  width: 280px;
  background-color: #ffffff;
  border-left: 1px solid #eee;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: width 0.3s ease;
  z-index: 5;
}

.sidebar-content {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.sidebar-section {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: flex 0.3s ease;
  position: relative;
}

.users-section {
  flex: 1;
  min-height: 200px;
  max-height: 50%;
  border-bottom: 1px solid #eee;
  overflow: hidden;
}

.summary-section {
  flex: 1;
  min-height: 200px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sidebar-header {
  padding: 15px;
  border-bottom: 1px solid #eee;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-shrink: 0;
  background-color: #fafafa;
  z-index: 2;
}

.sidebar-header h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #333;
}

.summary-status {
  font-size: 12px;
  color: #999;
  padding: 3px 8px;
  border-radius: 10px;
  background-color: #f5f5f5;
}

.summary-status.active {
  color: #52c41a;
  background-color: #f6ffed;
}

.user-list {
  flex: 1;
  overflow-y: auto;
  padding: 0;
  scrollbar-width: thin;
  scrollbar-color: #ddd #f5f5f5;
}

.user-list::-webkit-scrollbar {
  width: 6px;
}

.user-list::-webkit-scrollbar-track {
  background: #f5f5f5;
}

.user-list::-webkit-scrollbar-thumb {
  background-color: #ddd;
  border-radius: 3px;
}

.summary-content-container {
  flex: 1;
  overflow-y: auto;
  padding: 15px;
  position: relative;
  scrollbar-width: thin;
  scrollbar-color: #ddd #f5f5f5;
}

.summary-content-container::-webkit-scrollbar {
  width: 6px;
}

.summary-content-container::-webkit-scrollbar-track {
  background: #f5f5f5;
}

.summary-content-container::-webkit-scrollbar-thumb {
  background-color: #ddd;
  border-radius: 3px;
}

.auto-summary-content {
  color: #333;
  line-height: 1.6;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
}

.auto-summary-content h2 {
  margin-top: 0;
  font-size: 18px;
  color: #1677ff;
  margin-bottom: 15px;
  text-align: center;
  font-weight: 600;
  border-bottom: 2px solid #1677ff;
  padding-bottom: 10px;
  position: relative;
}

.auto-summary-content h3 {
  font-size: 16px;
  color: #fff;
  margin-top: 15px;
  margin-bottom: 10px;
  font-weight: 600;
  background: linear-gradient(135deg, #1677ff 0%, #4096ff 100%);
  padding: 8px 12px;
  border-radius: 6px;
  box-shadow: 0 2px 6px rgba(22, 119, 255, 0.2);
  position: relative;
  padding-left: 15px;
  display: flex;
  align-items: center;
}

.auto-summary-content ul {
  margin: 0 0 15px 0;
  padding: 0;
  background-color: #f9f9f9;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  overflow: hidden;
  border: 1px solid #eee;
}

.auto-summary-content li {
  margin: 0;
  padding: 8px 15px 8px 30px;
  position: relative;
  list-style-type: none;
  border-bottom: 1px solid #eee;
  transition: background-color 0.2s ease;
  font-size: 14px;
}

.auto-summary-content li:last-child {
  border-bottom: none;
}

.auto-summary-content li:hover {
  background-color: #f0f7ff;
}

.auto-summary-content li::before {
  content: "";
  position: absolute;
  width: 6px;
  height: 6px;
  background-color: #1677ff;
  left: 15px;
  top: 14px;
  border-radius: 50%;
}

.summary-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  min-height: 100px;
  padding: 20px 0;
}

.summary-loading .loading-spinner {
  width: 30px;
  height: 30px;
  border: 3px solid #f3f3f3;
  border-top: 3px solid #1677ff;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 15px;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.summary-loading p {
  color: #999;
  font-size: 14px;
}

.summary-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  min-height: 150px;
  color: #999;
  padding: 20px 0;
}

.summary-empty i {
  font-size: 40px;
  margin-bottom: 15px;
  opacity: 0.5;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='%23bbb'%3E%3Cpath d='M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm-2 17H8v-2h4v2zm6-4H6v-2h12v2zm0-4H6v-2h12v2z'/%3E%3C/svg%3E");
  width: 48px;
  height: 48px;
  background-size: contain;
  background-repeat: no-repeat;
}

.summary-empty p {
  margin: 5px 0;
  text-align: center;
}

.summary-hint {
  font-size: 12px;
  color: #bbb;
  margin-top: 5px;
}

.summary-error {
  background-color: #fff5f5;
  border-radius: 8px;
  padding: 15px;
  color: #cf1322;
  font-size: 14px;
  margin: 10px 0;
}

.user-item {
  display: flex;
  align-items: center;
  padding: 10px 15px;
  transition: background-color 0.3s ease;
  cursor: pointer;
}

.user-item:hover {
  background-color: #f5f7fa;
}

.user-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  overflow: hidden;
  margin-right: 10px;
}

.user-details {
  flex: 1;
  min-width: 0;
}

.user-name {
  font-size: 14px;
  font-weight: 500;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-status {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}

.user-status.online {
  color: #52c41a;
}

/* 底部输入区 */
.chat-footer {
  padding: 15px 20px;
  background-color: #ffffff;
  border-top: 1px solid #eee;
  display: flex;
  align-items: flex-end;
  z-index: 10;
}

.toolbar {
  display: flex;
  margin-right: 10px;
  padding-bottom: 8px;
}

.tool-button {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 15px;
  cursor: pointer;
  position: relative;
  border-radius: 6px;
  transition: all 0.2s ease; /* 减少过渡时间，提高响应速度 */
  background-color: #f0f2f5;
  user-select: none; /* 防止触摸设备上的长按选择 */
  touch-action: manipulation; /* 提高触摸响应速度 */
}

.tool-button:hover {
  background-color: #e0e3e9;
  transform: translateY(-2px);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
}

.emoji-icon, .image-icon, .file-icon {
  width: 24px;
  height: 24px;
  display: block;
}

.emoji-icon {
  background-color: #ffd666;
  border-radius: 50%;
  position: relative;
  box-shadow: 0 2px 4px rgba(255, 214, 102, 0.3);
}

.emoji-icon::before {
  content: "";
  position: absolute;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background-color: #fff;
  top: 7px;
  left: 7px;
}

.emoji-icon::after {
  content: "";
  position: absolute;
  width: 12px;
  height: 6px;
  border: 2px solid #fff;
  border-top: none;
  border-radius: 0 0 10px 10px;
  bottom: 5px;
  left: 6px;
}

.image-icon {
  background-color: #91d5ff;
  border-radius: 4px;
  position: relative;
  box-shadow: 0 2px 4px rgba(145, 213, 255, 0.3);
}

.image-icon::before {
  content: "";
  position: absolute;
  width: 18px;
  height: 14px;
  background-color: #fff;
  border-radius: 2px;
  top: 5px;
  left: 3px;
}

.image-icon::after {
  content: "";
  position: absolute;
  width: 6px;
  height: 6px;
  background-color: #91d5ff;
  border: 2px solid #fff;
  border-radius: 50%;
  top: 7px;
  left: 5px;
}

.file-icon {
  background-color: #b7eb8f;
  border-radius: 4px;
  position: relative;
  box-shadow: 0 2px 4px rgba(183, 235, 143, 0.3);
}

.file-icon::before {
  content: "";
  position: absolute;
  width: 14px;
  height: 18px;
  background-color: #fff;
  border-radius: 2px;
  top: 3px;
  left: 5px;
}

.file-icon::after {
  content: "";
  position: absolute;
  width: 8px;
  height: 2px;
  background-color: #b7eb8f;
  top: 8px;
  left: 8px;
  box-shadow: 0 3px 0 #b7eb8f, 0 6px 0 #b7eb8f;
}

.file-input {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  opacity: 0;
  font-size: 0; /* 防止出现意外的可点击区域 */
  pointer-events: none; /* 禁用输入框的直接交互，由按钮控制 */
}

.input-container {
  flex: 1;
  position: relative;
}

.emoji-panel {
  position: absolute;
  bottom: 100%;
  left: 0;
  background-color: #ffffff;
  width: 280px;
  height: 200px;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  padding: 10px;
  display: flex;
  flex-wrap: wrap;
  overflow-y: auto;
  margin-bottom: 10px;
  z-index: 100;
  animation: fadeIn 0.2s ease;
}

.emoji-item {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 20px;
  border-radius: 4px;
  transition: background-color 0.2s ease;
}

.emoji-item:hover {
  background-color: #f5f7fa;
}

.message-input {
  width: 100%;
  min-height: 60px;
  max-height: 120px;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  padding: 10px 15px;
  font-size: 14px;
  resize: none;
  line-height: 1.5;
  outline: none;
  transition: border-color 0.3s ease, box-shadow 0.3s ease;
}

.message-input:focus {
  border-color: #1677ff;
  box-shadow: 0 0 0 2px rgba(22, 119, 255, 0.1);
}

.send-button {
  margin-left: 15px;
  padding: 0 20px;
  height: 36px;
  background-color: #1677ff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  display: flex;
  align-items: center;
  transition: background-color 0.3s ease;
}

.send-button:hover {
  background-color: #4096ff;
}

.send-button:disabled {
  background-color: #d9d9d9;
  cursor: not-allowed;
}

.send-icon {
  margin-right: 6px;
  width: 16px;
  height: 16px;
  position: relative;
  display: inline-block;
}

.send-icon::before {
  content: "";
  position: absolute;
  top: 8px;
  left: 0;
  width: 14px;
  height: 1px;
  background-color: white;
}

.send-icon::after {
  content: "";
  position: absolute;
  top: 4px;
  left: 8px;
  width: 8px;
  height: 8px;
  border: 1px solid white;
  border-left: none;
  border-bottom: none;
  transform: rotate(45deg);
}

/* 图片预览弹窗 */
.image-preview-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  animation: fadeIn 0.3s ease;
}

.image-preview-content {
  position: relative;
  max-width: 90%;
  max-height: 90%;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.image-preview-content img {
  max-width: 100%;
  max-height: 80vh;
  display: block;
}

.close-preview {
  position: absolute;
  top: -20px;
  right: -20px;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: rgba(0, 0, 0, 0.5);
  color: white;
  border: none;
  font-size: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.close-preview:hover {
  background-color: rgba(0, 0, 0, 0.8);
}

/* 通知提示 */
.notification {
  position: fixed;
  top: 20px;
  left: 50%;
  transform: translateX(-50%);
  padding: 10px 20px;
  border-radius: 4px;
  color: white;
  font-size: 14px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  z-index: 1000;
  animation: slideDown 0.3s ease, fadeOut 0.3s ease 2.7s;
}

@keyframes slideDown {
  from {
    transform: translateX(-50%) translateY(-20px);
    opacity: 0;
  }
  to {
    transform: translateX(-50%) translateY(0);
    opacity: 1;
  }
}

@keyframes fadeOut {
  from {
    opacity: 1;
  }
  to {
    opacity: 0;
  }
}

.notification.info {
  background-color: #1677ff;
}

.notification.success {
  background-color: #52c41a;
}

.notification.warning {
  background-color: #faad14;
}

.notification.error {
  background-color: #ff4d4f;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .sidebar {
    position: absolute;
    right: 0;
    height: 100%;
    transform: translateX(100%);
    transition: transform 0.3s ease;
    z-index: 15;
  }
  
  .sidebar.active {
    transform: translateX(0);
  }
  
  .message-content {
    max-width: 80%;
  }
}

/* 聊天总结按钮 */
.summary-button {
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  border-radius: 6px;
  padding: 8px 14px;
  font-size: 14px;
  margin-right: 15px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.summary-button:hover:not(:disabled) {
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
  transform: translateY(-2px);
}

.summary-button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.summary-icon {
  display: inline-block;
  width: 16px;
  height: 16px;
  margin-right: 6px;
  position: relative;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='white'%3E%3Cpath d='M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm-2 17H8v-2h4v2zm6-4H6v-2h12v2zm0-4H6v-2h12v2z'/%3E%3C/svg%3E");
  background-size: contain;
  background-repeat: no-repeat;
}

/* 聊天总结弹窗 */
.summary-modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  animation: fadeIn 0.3s ease;
}

.summary-modal-content {
  width: 700px;
  max-width: 90vw;
  max-height: 80vh;
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
  display: flex;
  flex-direction: column;
  animation: scaleIn 0.3s ease;
  overflow: hidden;
}

.summary-modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid #eaeaea;
}

.summary-modal-header h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.close-summary {
  background: none;
  border: none;
  color: #999;
  font-size: 24px;
  cursor: pointer;
  line-height: 24px;
  padding: 0;
  margin: 0;
  transition: color 0.2s;
}

.close-summary:hover {
  color: #333;
}

.summary-modal-body {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
}

.summary-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
}

.summary-loading .loading-spinner {
  width: 40px;
  height: 40px;
  margin-bottom: 20px;
}

.summary-loading p {
  color: #666;
  text-align: center;
}

.summary-error {
  padding: 20px;
  background-color: #fff5f5;
  border-radius: 8px;
  border-left: 4px solid #ff4d4f;
  color: #cf1322;
}

.summary-content {
  color: #333;
  line-height: 1.6;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
}

.summary-content h2 {
  margin-top: 0;
  font-size: 24px;
  color: #1677ff;
  margin-bottom: 20px;
  text-align: center;
  font-weight: 600;
  border-bottom: 2px solid #1677ff;
  padding-bottom: 15px;
  position: relative;
}

.summary-content h2::after {
  content: "";
  position: absolute;
  width: 40px;
  height: 4px;
  background-color: #1677ff;
  left: 50%;
  bottom: -2px;
  transform: translateX(-50%);
  border-radius: 2px;
}

.summary-content h3 {
  font-size: 18px;
  color: #fff;
  margin-top: 20px;
  margin-bottom: 12px;
  font-weight: 600;
  background: linear-gradient(135deg, #1677ff 0%, #4096ff 100%);
  padding: 10px 15px;
  border-radius: 6px;
  box-shadow: 0 2px 6px rgba(22, 119, 255, 0.2);
  position: relative;
  padding-left: 18px;
  display: flex;
  align-items: center;
}

.summary-content h3::before {
  content: "";
  position: absolute;
  left: 8px;
  width: 4px;
  height: 18px;
  background-color: #fff;
  border-radius: 2px;
}

.summary-content ul {
  margin: 0 0 25px 0;
  padding: 0;
  background-color: #f9f9f9;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  overflow: hidden;
  border: 1px solid #eee;
}

.summary-content li {
  margin: 0;
  padding: 12px 15px 12px 40px;
  position: relative;
  list-style-type: none;
  border-bottom: 1px solid #eee;
  transition: background-color 0.2s ease;
}

.summary-content li:last-child {
  border-bottom: none;
}

.summary-content li:hover {
  background-color: #f0f7ff;
}

.summary-content li::before {
  content: "";
  position: absolute;
  width: 6px;
  height: 6px;
  background-color: #1677ff;
  left: 20px;
  top: 18px;
  border-radius: 50%;
}

.summary-content li::after {
  content: "";
  position: absolute;
  width: 3px;
  height: 100%;
  background-color: #e6f0ff;
  left: 21.5px;
  top: 0;
  z-index: 0;
}

.summary-content li:first-child::after {
  top: 18px;
  height: calc(100% - 18px);
}

.summary-content li:last-child::after {
  height: 18px;
}

.summary-note {
  margin-top: 20px;
  padding: 15px 20px;
  background-color: #fffbe6;
  border-left: 4px solid #faad14;
  border-radius: 4px;
  color: #876800;
  font-size: 14px;
  box-shadow: 0 2px 8px rgba(250, 173, 20, 0.1);
}

/* 动画 */
@keyframes scaleIn {
  from {
    opacity: 0;
    transform: scale(0.9);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

/* 响应式 */
@media (max-width: 768px) {
  .summary-modal-content {
    width: 95vw;
    max-height: 85vh;
  }
}

.chat-actions {
  display: flex;
  gap: 0.5rem;
}

/* 添加禁用状态 */
.tool-button.disabled {
  opacity: 0.5;
  cursor: not-allowed;
  pointer-events: none;
}

/* 修改头像样式 */
.avatar-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
}

/* 调整自己的消息样式 */
.self-message {
  flex-direction: row-reverse;
}

.self-message .message-content {
  align-items: flex-end;
  text-align: right;
}

.message-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  overflow: hidden;
  margin: 0 10px;
  flex-shrink: 0;
  cursor: pointer;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.self-avatar {
  margin-left: 10px;
  margin-right: 0;
}

.vip-avatar {
  border: 2px solid #ffd700;
}

.svip-avatar {
  border: 2px solid #ff4500;
}

.vip-tag, .svip-tag {
  font-size: 12px;
  color: #fff;
  padding: 2px 5px;
  border-radius: 4px;
  margin-left: 5px;
}

.vip-tag {
  background-color: #ffd700;
}

.svip-tag {
  background-color: #ff4500;
}

</style>  