<template>
  <div class="private-chat-container">
    <!-- 头部信息 -->
    <header class="chat-header">
      <div class="chat-info">
        <div class="avatar-container">
          <img v-if="userAvatar" :src="userAvatar" class="avatar" :alt="username" @error="handleImageError"/>
          <div v-else class="avatar">{{ username ? username.charAt(0).toUpperCase() : 'U' }}</div>
          <!-- 状态指示器可以保留，或者根据自己的在线状态调整 -->
          <!-- <div class="status-indicator" :class="friendInfo.status || 'offline'"></div> -->
        </div>
        <div class="user-details">
          <div class="username">{{ username || '我的账户' }}</div>
          <!-- 状态文本可以修改为显示自己的状态，或移除 -->
          <!-- <div class="status-text">{{ friendInfo.status === 'online' ? '在线' : '离线' }}</div> -->
          <!-- 签名通常是好友的，自己的签名可以考虑从其他地方获取或不显示 -->
          <!-- <div v-if="friendInfo.signature" class="signature">{{ friendInfo.signature }}</div> -->
        </div>
      </div>
      <div class="header-actions">
        <button class="action-button leave-button" @click="goBack">
          退出
        </button>
      </div>
    </header>
    
    <!-- 主内容区 -->
    <main class="chat-main">
      <!-- 好友列表侧边栏 -->
      <div class="sidebar">
        <div class="sidebar-content">
          <!-- 好友列表部分 -->
          <div class="sidebar-section friends-section">
            <div class="sidebar-header">
              <h2>我的好友</h2>
              <div class="search-box">
                <input 
                  type="text" 
                  v-model="friendSearch" 
                  placeholder="搜索好友..." 
                  @input="handleFriendSearch"
                >
              </div>
            </div>
            <div class="friends-list">
              <div 
                v-for="friend in filteredFriends" 
                :key="friend.userId" 
                class="friend-item"
                :class="{ 'active': currentFriendId && currentFriendId === friend.userId }"
                @click="selectFriend(friend)"
              >
                <div class="friend-avatar">
                  <img v-if="friend.avatar" :src="friend.avatar" :alt="friend.username" />
                  <span v-else class="default-avatar">{{ friend?.username?.charAt(0)?.toUpperCase() || '?' }}</span>
                  <div class="status-indicator" :class="friend.status || 'offline'"></div>
                </div>
                <div class="friend-details">
                  <div class="friend-name">{{ friend.username }}</div>
                  <div class="friend-status">{{ friend.status === 'online' ? '在线' : '离线' }}</div>
                </div>
              </div>
              
              <div v-if="filteredFriends.length === 0" class="empty-friends">
                <i class="friends-icon"></i>
                <p>暂无好友</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 聊天消息区 -->
      <div class="chat-content" ref="chatContent">
        <div v-if="!currentFriendId" class="empty-chat-message">
          <i class="fas fa-comments"></i>
          <p>请选择一个好友开始聊天</p>
        </div>
        <div v-else-if="messages.length === 0" class="empty-chat-message">
          <i class="fas fa-comments"></i>
          <p>还没有消息，开始聊天吧！</p>
        </div>
        
        <div v-else class="messages-wrapper">
          <template v-for="(message, index) in messages" :key="message.messageId || index">
            <div v-if="shouldShowDateSeparator(index)" class="date-separator">
              <span>{{ formatDate(message.sendTime || message.createTime) }}</span>
            </div>
            <div class="message-item" :class="{'self-message': message.senderId === userId}">
              <!-- 头像容器 -->
              <div class="message-avatar-container">
                <!-- 对方头像 -->
                <div class="avatar-display" v-if="message.senderId !== userId">
                  <img v-if="message.senderAvatar" :src="message.senderAvatar" class="avatar-img" :alt="message.senderName" @error="handleImageError" />
                  <img v-else-if="friendInfo.avatar" :src="friendInfo.avatar" class="avatar-img" :alt="friendInfo.username" @error="handleImageError" />
                  <div v-else class="avatar-default">
                    {{ (message.senderName || '?')?.charAt(0)?.toUpperCase() || '?' }}
                  </div>
                </div>
                <!-- 自己头像 -->
                <div class="avatar-display" v-if="message.senderId === userId">
                  <img v-if="message.senderAvatar" :src="message.senderAvatar" class="avatar-img" :alt="username" @error="handleImageError" />
                  <img v-else-if="userAvatar" :src="userAvatar" class="avatar-img" :alt="username" @error="handleImageError" />
                  <div v-else class="avatar-default">
                    {{ username.charAt(0).toUpperCase() || '?' }}
                  </div>
                </div>
              </div>

              <!-- 消息主内容区域 (头部 + 实际消息体) -->
              <div class="message-content-area">
                <div class="message-header">
                  <span class="message-sender" v-if="message.senderId !== userId">
                    {{ message.senderName || '未知用户' }}
                  </span>
                  <span class="message-time">{{ formatTime(message.sendTime || message.createTime) }}</span>
                </div>

                <div class="message-body"> <!-- 消息气泡 -->
                  <div v-if="message.messageType === 'text'" class="message-text">
                    {{ message.content }}
                  </div>
                  <div v-else-if="message.messageType === 'image'" class="message-image">
                    <img
                      :src="getFullImageUrl(message.fileUrl)"
                      alt="图片消息"
                      @click="previewImage(message.fileUrl)"
                      @error="handleImageError"
                      loading="lazy" />
                    <div class="image-info">{{ message.fileName || '图片' }} ({{ formatFileSize(message.fileSize) }})</div>
                  </div>
                  <div v-else-if="message.messageType === 'file'" class="message-file" @click="downloadFile(message.fileUrl, message.fileName)">
                    <div class="file-icon"><i class="fas fa-file-alt"></i></div>
                    <div class="file-info">
                      <div class="file-name">{{ message.fileName || '文件' }}</div>
                      <div class="file-size">{{ formatFileSize(message.fileSize) }}</div>
                    </div>
                    <div class="download-icon"><i class="fas fa-download"></i></div>
                  </div>
                  <div v-else-if="message.messageType === 'emoji'" class="message-emoji">
                    {{ message.content }}
                  </div>
                </div>
              </div>
            </div>
          </template>
        </div>
      </div>
    </main>
    
    <!-- 底部输入区 -->
    <footer class="chat-footer" v-if="currentFriendId">
      <div class="toolbar">
        <div class="tool-button emoji-button" @click="toggleEmojiPanel">
          <i class="fas fa-smile"></i>
        </div>
        <div class="tool-button image-button" @click="triggerImageUpload">
          <i class="fas fa-image"></i>
          <input 
            type="file" 
            ref="imageInput" 
            accept="image/*" 
            style="display: none" 
            @change="handleImageUpload"
          >
        </div>
        <div class="tool-button file-button">
          <input
            type="file"
            ref="fileInput"
            @change="handleFileUpload"
            style="display: none"
          />
          <i class="fas fa-paperclip" @click="triggerFileInput"></i>
        </div>
      </div>
      
      <div v-show="showEmojiPanel" class="emoji-panel">
        <div v-for="emoji in emojis" 
             :key="emoji" 
             class="emoji-item" 
             @click="insertEmoji(emoji)">
          {{ emoji }}
        </div>
      </div>
      
      <textarea 
        v-model="newMessage" 
        placeholder="请输入消息..." 
        @keyup.enter.exact.prevent="sendMessage"
        @keydown.ctrl.enter="addNewLine"
        ref="messageInput"></textarea>
        
      <button @click="sendMessage" :disabled="!newMessage.trim() || !isConnected">
        <i class="fas fa-paper-plane"></i>
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
    <div v-if="showingUserCard" class="user-card-modal">
      <div class="user-card-content">
        <div class="user-card-header">
          <h3>{{ friendInfo.username }}</h3>
          <button class="close-card" @click="closeUserCard">×</button>
        </div>
        <div class="user-card-body">
          <div class="user-info">
            <div class="avatar-container">
              <img :src="friendInfo.avatar" alt="用户头像" />
            </div>
            <div class="user-details">
              <div class="username">{{ friendInfo.username }}</div>
              <div class="status-text">{{ friendInfo.status === 'online' ? '在线' : '离线' }}</div>
              <div v-if="friendInfo.signature" class="signature">{{ friendInfo.signature }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, onUnmounted, nextTick, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import axios from 'axios';
import * as signalR from '@microsoft/signalr';
import UserCard from '@/components/UserCard.vue';

export default {
  name: 'PrivateChat',
  components: {
    UserCard
  },
  setup() {
    const route = useRoute();
    const router = useRouter();
    const userId = ref(parseInt(localStorage.getItem('userId') || '0'));
    const username = ref(localStorage.getItem('username') || '访客');
    const userAvatar = ref(localStorage.getItem('userAvatar') || '');
    
    // 获取初始friendId (如果URL中有)
    const initialFriendId = route.params.id ? parseInt(route.params.id) : null;
    const currentFriendId = ref(initialFriendId);
    
    const friendInfo = ref({
      username: '',
      avatar: '',
      status: 'offline',
      signature: ''
    });
    
    const friends = ref([]);
    const filteredFriends = ref([]);
    const friendSearch = ref('');
    
    const messages = ref([]);
    const newMessage = ref('');
    const chatContent = ref(null);
    const messageInput = ref(null);
    const isConnected = ref(false);
    const connection = ref(null);
    
    const showEmojiPanel = ref(false);
    const emojis = ref(['😀', '😃', '😄', '😁', '😆', '😅', '😂', '🤣', '🥲', '☺️', '😊', '😇', 
                      '🙂', '🙃', '😉', '😌', '😍', '🥰', '😘', '😗', '😙', '😚', '😋', '😛',
                      '😝', '😜', '🤪', '🤨', '🧐', '🤓', '😎', '🤩', '🥳', '😏', '😒', '😞',
                      '😔', '😟', '😕', '🙁', '☹️', '😣', '😖', '😫', '😩', '🥺', '😢', '😭',
                      '😤', '😠', '😡', '🤬', '🤯', '😳', '🥵', '🥶', '😱', '😨', '😰', '😥']);
    
    const fileInput = ref(null);
    const imageInput = ref(null);
    
    // 图片预览
    const previewImageUrl = ref(null);
    
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
    
    const loadFriends = async () => {
      try {
        const response = await axios.get(
          `${window.apiBaseUrl}/api/user/${userId.value}/friends`,
          {
            headers: {
              'UserId': userId.value.toString()
            }
          }
        );
        
        if (response.data && response.data.code === 200 && response.data.data) {
          friends.value = response.data.data.map(friend => {
            let avatarUrl = friend.avatar || '';
            if (avatarUrl && !avatarUrl.startsWith('http')) {
              avatarUrl = avatarUrl.startsWith('/') ? avatarUrl : `/${avatarUrl}`;
              avatarUrl = `${window.apiBaseUrl}${avatarUrl}`;
            }
            return {
              ...friend,
              userId: friend.userId || friend.id,
              avatar: avatarUrl,
              status: friend.status || 'offline',
              signature: friend.signature || ''
            };
          });
          filteredFriends.value = [...friends.value];
          console.log('好友列表已加载:', friends.value);
        } else {
          console.error('获取好友列表失败:', response.data?.msg || '未知错误');
        }
      } catch (error) {
        console.error('加载好友列表失败:', error);
      }
    };
    
    // 搜索好友
    const handleFriendSearch = () => {
      if (!friendSearch.value.trim()) {
        filteredFriends.value = [...friends.value];
        return;
      }
      
      const search = friendSearch.value.toLowerCase();
      filteredFriends.value = friends.value.filter(friend => 
        friend.username.toLowerCase().includes(search)
      );
    };
    
    // 选择好友
    const selectFriend = async (friend) => {
      if (!friend || !friend.userId) {
        console.error('选择的好友数据无效:', friend);
        return;
      }
      
      if (currentFriendId.value === friend.userId) return;
      
      currentFriendId.value = friend.userId;
      friendInfo.value = {
        username: friend.username,
        avatar: friend.avatar,
        status: friend.status || 'offline',
        signature: friend.signature
      };
      
      // 更新URL，但不触发页面重载
      router.push({
        name: 'PrivateChat',
        params: { id: String(friend.userId) },
        replace: true
      });
      
      // 清空当前消息
      messages.value = [];
      
      // 加入新的私聊
      if (connection.value && isConnected.value) {
        try {
          await connection.value.invoke('JoinPrivateChat', friend.userId);
          await loadChatHistory();
        } catch (error) {
          console.error('加入私聊或加载历史记录失败:', error);
          showNotification('连接聊天服务失败，请刷新页面重试', 'error');
        }
      }
    };
    
    // 监听路由变化
    watch(() => route.params.id, async (newId) => {
      if (newId && parseInt(newId) !== currentFriendId.value) {
        const friendId = parseInt(newId);
        currentFriendId.value = friendId;
        
        // 立即获取好友信息
        await loadFriendInfo(friendId);
        
        // 加入新的私聊
        if (connection.value && isConnected.value) {
          await connection.value.invoke('JoinPrivateChat', friendId);
          await loadChatHistory();
        }
      }
    }, { immediate: true });
    
    const loadChatHistory = async () => {
      try {
        if (!currentFriendId.value) {
          console.warn('未选择好友，无法加载聊天历史');
          return;
        }
        
        console.log('正在加载与好友 ID:', currentFriendId.value, '的聊天历史');
        
        // 使用SignalR获取历史消息
        if (connection.value && isConnected.value) {
          try {
            await connection.value.invoke('GetPrivateChatHistory', currentFriendId.value, 50);
          } catch (error) {
            console.error('通过SignalR获取历史消息失败:', error);
            showNotification('获取历史消息失败，请刷新页面重试', 'error');
          }
        } else {
          console.warn('SignalR连接未建立，无法获取历史消息');
          showNotification('连接未建立，请稍后重试', 'warning');
        }
      } catch (error) {
        console.error('获取历史消息失败:', error);
        showNotification('获取历史消息失败', 'error');
      }
    };
    
    const loadFriendInfo = async (friendId) => {
      try {
        if (!friendId) return;
        
        // 先从已有好友列表查找
        const friend = friends.value.find(f => f.userId === friendId);
        if (friend && friend.avatar) {
          friendInfo.value = {
            username: friend.username,
            avatar: friend.avatar,
            status: friend.status || 'offline',
            signature: friend.signature
          };
          console.log('从好友列表获取到好友信息:', friendInfo.value);
          return;
        }
        
        console.log('正在从API获取好友信息, ID:', friendId);
        const response = await axios.get(
          `${window.apiBaseUrl}/api/user/${friendId}`,
          {
            headers: {
              'UserId': userId.value.toString()
            }
          }
        );
        
        if (response.data && response.data.code === 200 && response.data.data) {
          // 处理头像URL
          let avatarUrl = response.data.data.avatar || '';
          if (avatarUrl && !avatarUrl.startsWith('http')) {
            avatarUrl = avatarUrl.startsWith('/') ? avatarUrl : `/${avatarUrl}`;
            avatarUrl = `${window.apiBaseUrl}${avatarUrl}`;
          }
          
          friendInfo.value = {
            username: response.data.data.username,
            avatar: avatarUrl,
            status: response.data.data.status || 'offline',
            signature: response.data.data.signature
          };
          console.log('从API获取到好友信息:', friendInfo.value);
          
          // 更新本地好友列表中的信息
          const existingFriend = friends.value.find(f => f.userId === friendId);
          if (existingFriend) {
            existingFriend.avatar = avatarUrl;
            existingFriend.status = response.data.data.status;
            existingFriend.signature = response.data.data.signature;
          }
        } else {
          console.error('获取好友信息失败:', response.data?.msg || '未知错误');
          showNotification('获取好友信息失败', 'error');
        }
      } catch (error) {
        console.error('获取好友信息失败:', error);
        showNotification('获取好友信息失败', 'error');
      }
    };
    
    const setupSignalR = async () => {
      try {
        connection.value = new signalR.HubConnectionBuilder()
          .withUrl(`${window.apiBaseUrl}/privateChatHub?userId=${userId.value}`)
          .withAutomaticReconnect()
          .build();
        
        // 注册连接
        connection.value.on('ReceivePrivateMessage', (message) => {
          console.log('收到私聊消息:', message);
          // 只显示当前聊天对象的消息
          if ((message.senderId === currentFriendId.value && message.receiverId === userId.value) || 
              (message.senderId === userId.value && message.receiverId === currentFriendId.value)) {
            // 确保消息有适当的头像信息
            if (message.senderId === currentFriendId.value) {
              message.senderAvatar = friendInfo.value.avatar;
            } else if (message.senderId === userId.value) {
              message.senderAvatar = userAvatar.value;
            }
            
            // 处理表情消息
            if (message.messageType === 'emoji' && message.content) {
              try {
                // 尝试使用JSON解析，如果失败则使用原始内容
                const decodedEmoji = JSON.parse(`"${message.content}"`);
                message.content = decodedEmoji;
              } catch (e) {
                console.warn('实时表情解码失败:', e);
                // 保持原样
              }
            }
            
            messages.value.push(message);
            nextTick(() => scrollToBottom());
          }
        });
        
        // 接收历史消息
        connection.value.on('ReceiveHistoryMessages', (historyMessages) => {
          console.log('收到历史消息:', historyMessages);
          if (Array.isArray(historyMessages)) {
            messages.value = historyMessages.map(msg => {
              // 添加时间戳和头像信息
              const message = {
                ...msg,
                sendTime: msg.sendTime || msg.createTime || new Date().toISOString() // 确保时间戳存在
              };
              
              // 为消息添加头像信息
              if (message.senderId === currentFriendId.value) {
                const friend = friends.value.find(f => f.userId === currentFriendId.value);
                message.senderAvatar = friend?.avatar || friendInfo.value.avatar;
                message.senderName = friend?.username || friendInfo.value.username;
              } else if (message.senderId === userId.value) {
                message.senderAvatar = userAvatar.value;
                message.senderName = username.value;
              }
              
              // 处理图片消息的URL
              if (message.messageType === 'image' && message.fileUrl) {
                message.fileUrl = formatMessageUrl(message.fileUrl);
              }

              // 确保表情消息正确显示
              if (message.messageType === 'emoji' && message.content) {
                // 确保表情内容正确解码
                try {
                  // 尝试使用JSON解析，如果失败则使用原始内容
                  const decodedEmoji = JSON.parse(`"${message.content}"`);
                  message.content = decodedEmoji;
                } catch (e) {
                  console.warn('表情解码失败:', e);
                  // 保持原样
                }
              }
              
              // 如果还是没有头像，尝试加载用户信息
              if (!message.senderAvatar) {
                loadFriendInfo(message.senderId).then(() => {
                  const friend = friends.value.find(f => f.userId === message.senderId);
                  if (friend) {
                    message.senderAvatar = friend.avatar;
                    message.senderName = friend.username;
                  }
                }).catch(err => console.error('加载用户头像失败:', err));
              }
              
              return message;
            });
            nextTick(() => scrollToBottom());
          } else {
            console.warn('历史消息数据格式不正确:', historyMessages);
          }
        });
        
        // 监听用户状态变化
        connection.value.on('UserStatusChanged', (changedUserId, status) => {
          console.log('用户状态变化:', changedUserId, status);
          // 更新好友列表中的状态
          const friend = friends.value.find(f => f.userId === changedUserId);
          if (friend) {
            friend.status = status;
          }
          
          // 如果是当前聊天的好友，更新头部信息
          if (changedUserId === currentFriendId.value) {
            friendInfo.value.status = status;
          }
        });
        
        // 错误处理
        connection.value.on('Error', (error) => {
          console.error('SignalR错误:', error);
          showNotification(error, 'error');
        });
        
        try {
          await connection.value.start();
          console.log('SignalR连接已建立');
          isConnected.value = true;
          
          // 注册连接
          await connection.value.invoke('RegisterConnection', userId.value);
          
          // 如果有选中的好友，加入私聊
          if (currentFriendId.value) {
            await connection.value.invoke('JoinPrivateChat', currentFriendId.value);
            await loadChatHistory();
          }
        } catch (startError) {
          console.error('SignalR连接启动失败:', startError);
          showNotification('聊天服务连接失败，请刷新页面重试', 'error');
          isConnected.value = false;
        }
        
      } catch (error) {
        console.error('SignalR连接失败:', error);
        showNotification('连接失败，请刷新页面重试', 'error');
        isConnected.value = false;
      }
    };

    const sendMessage = async () => {
      if (!newMessage.value.trim() || !isConnected.value || !currentFriendId.value) return;
      
      try {
        const message = {
          senderId: userId.value,
          senderName: username.value,
          senderAvatar: userAvatar.value,
          receiverId: currentFriendId.value,
          content: newMessage.value.trim(),
          messageType: 'text',
          sendTime: new Date().toISOString()
        };
        
        // 检查是否是表情消息
        if (emojis.value.includes(newMessage.value.trim())) {
          message.messageType = 'emoji';
          // 确保表情符号以UTF-8格式正确存储
          message.content = newMessage.value.trim();
        }
        
        await connection.value.invoke('SendPrivateMessage', message);
        
        newMessage.value = '';
        
        messageInput.value?.focus();
      } catch (error) {
        console.error('发送消息失败:', error);
        showNotification('发送消息失败，请重试', 'error');
      }
    };
    
    const goBack = () => {
      router.push('/home');
    };
    
    const insertEmoji = (emoji) => {
      newMessage.value += emoji;
      showEmojiPanel.value = false;
      messageInput.value?.focus();
    };
    
    const toggleEmojiPanel = () => {
      showEmojiPanel.value = !showEmojiPanel.value;
      if (showEmojiPanel.value) {
        // 确保表情面板显示在正确的位置
        nextTick(() => {
          const emojiPanel = document.querySelector('.emoji-panel');
          if (emojiPanel) {
            const inputRect = document.querySelector('.chat-input').getBoundingClientRect();
            emojiPanel.style.bottom = `${inputRect.height}px`;
          }
        });
      }
    };
    
    // 点击其他地方时关闭表情面板
    const handleClickOutside = (event) => {
      const emojiButton = document.querySelector('.emoji-button');
      const emojiPanel = document.querySelector('.emoji-panel');
      
      if (emojiPanel && 
          !emojiPanel.contains(event.target) && 
          !emojiButton.contains(event.target)) {
        showEmojiPanel.value = false;
      }
    };
    
    const addNewLine = () => {
      newMessage.value += '\n';
    };
    
    const scrollToBottom = () => {
      if (chatContent.value) {
        chatContent.value.scrollTop = chatContent.value.scrollHeight;
      }
    };
    
    const formatDate = (dateString) => {
      if (!dateString) return '';
      
      try {
        // 直接创建Date对象
        const date = new Date(dateString);
        if (isNaN(date.getTime())) return '';
        
        // 计算北京时间
        const localTime = new Date();
        const localOffset = localTime.getTimezoneOffset() * 60000; // 本地时区偏移（毫秒）
        const beijingOffset = 8 * 60 * 60000; // 北京时区偏移UTC+8（毫秒）
        const beijingTime = new Date(date.getTime() + localOffset + beijingOffset);
        
        // 获取当前的北京时间用于对比
        const nowBJ = new Date(Date.now() + localOffset + beijingOffset);
        const yesterdayBJ = new Date(nowBJ);
        yesterdayBJ.setDate(nowBJ.getDate() - 1);
        
        if (beijingTime.toDateString() === nowBJ.toDateString()) {
          return '今天';
        } else if (beijingTime.toDateString() === yesterdayBJ.toDateString()) {
          return '昨天';
        } else {
          return beijingTime.toLocaleDateString('zh-CN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit'
          });
        }
      } catch (error) {
        console.error('日期格式化错误:', error);
        return '';
      }
    };
    
    const formatTime = (dateString) => {
      if (!dateString) return '';
      
      try {
        // 直接创建Date对象
        const date = new Date(dateString);
        if (isNaN(date.getTime())) return '';
        
        // 计算北京时间
        const localTime = new Date();
        const localOffset = localTime.getTimezoneOffset() * 60000; // 本地时区偏移（毫秒）
        const beijingOffset = 8 * 60 * 60000; // 北京时区偏移UTC+8（毫秒）
        const beijingTime = new Date(date.getTime() + localOffset + beijingOffset);
        
        // 格式化为北京时间
        return beijingTime.toLocaleTimeString('zh-CN', {
          hour: '2-digit',
          minute: '2-digit',
          hour12: false
        });
      } catch (error) {
        console.error('时间格式化错误:', error);
        return '';
      }
    };
    
    const formatFileSize = (bytes) => {
      if (!bytes) return '未知大小';
      const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
      if (bytes === 0) return '0 B';
      const i = Math.floor(Math.log(bytes) / Math.log(1024));
      return (bytes / Math.pow(1024, i)).toFixed(2) + ' ' + sizes[i];
    };
    
    const shouldShowDateSeparator = (index) => {
      if (index === 0) return true;
      
      try {
        const currentMsg = messages.value[index];
        const prevMsg = messages.value[index - 1];
        
        if (!currentMsg || !prevMsg) return false;
        
        const currentTime = currentMsg.sendTime || currentMsg.createTime;
        const prevTime = prevMsg.sendTime || prevMsg.createTime;
        
        if (!currentTime || !prevTime) return false;
        
        const currentDate = new Date(currentTime).setHours(0, 0, 0, 0);
        const prevDate = new Date(prevTime).setHours(0, 0, 0, 0);
        
        if (isNaN(currentDate) || isNaN(prevDate)) return false;
        
        return currentDate !== prevDate;
      } catch (error) {
        console.error('日期分隔符计算错误:', error);
        return false;
      }
    };
    
    const showNotification = (message, type = 'info') => {
      console.log(`${type}: ${message}`);
    };
    
    const triggerFileInput = () => {
      fileInput.value.click();
    };
    
    const handleFileUpload = async (event) => {
      if (!event.target.files || event.target.files.length === 0) {
        return;
      }
      
      const file = event.target.files[0];
      const formData = new FormData();
      formData.append('file', file);
      
      try {
        showNotification('文件上传中...', 'info');
        
        const response = await axios.post(`${window.apiBaseUrl}/api/file/upload`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
        await connection.value.invoke(
          'SendFileToPrivate', 
          currentFriendId.value,
          response.data.url,
          response.data.fileName,
          response.data.fileSize
        );
        
        fileInput.value.value = '';
        showNotification('文件发送成功', 'success');
      } catch (error) {
        console.error('文件上传失败:', error);
        showNotification('文件上传失败: ' + (error.response?.data || error.message), 'error');
        fileInput.value.value = '';
      }
    };
    
    const downloadFile = async (fileUrl, fileName) => {
      try {
        const response = await axios.get(fileUrl, {
          responseType: 'blob'
        });
        
        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', fileName);
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      } catch (error) {
        console.error('文件下载失败:', error);
        showNotification('文件下载失败', 'error');
      }
    };
    
    // 图片预览相关方法
    const previewImage = (url) => {
      previewImageUrl.value = url;
    };
    
    const closeImagePreview = () => {
      previewImageUrl.value = null;
    };
    
    const triggerImageUpload = () => {
      imageInput.value.click();
    };
    
    const handleImageUpload = async (event) => {
      if (!event.target.files || event.target.files.length === 0) {
        return;
      }
      
      const file = event.target.files[0];
      const formData = new FormData();
      formData.append('file', file);
      
      try {
        showNotification('图片上传中...', 'info');
        
        const response = await axios.post(`${window.apiBaseUrl}/api/file/upload`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        
        if (response.data && response.data.url) {
          // 确保URL是完整的
          const imageUrl = response.data.url.startsWith('http') 
            ? response.data.url 
            : `${window.apiBaseUrl}${response.data.url.startsWith('/') ? '' : '/'}${response.data.url}`;
            
          await connection.value.invoke(
            'SendImageToPrivate', 
            currentFriendId.value,
            imageUrl, 
            response.data.fileName, 
            response.data.fileSize
          );
          
          imageInput.value.value = '';
          showNotification('图片发送成功', 'success');
        } else {
          throw new Error('图片上传失败：服务器返回数据格式不正确');
        }
      } catch (error) {
        console.error('图片上传失败:', error);
        showNotification('图片上传失败: ' + (error.response?.data?.message || error.message), 'error');
        imageInput.value.value = '';
      }
    };
    
    // 修改消息显示部分
    const formatMessageUrl = (url) => {
      if (!url) return '';
      if (url.startsWith('http')) return url;
      return `${window.apiBaseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
    };
    
    // 处理图片URL
    const getFullImageUrl = (url) => {
      if (!url) return null;
      if (url.startsWith('http')) return url;
      return `${window.apiBaseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
    };
    
    // 添加图片加载错误处理
    const handleImageError = (event) => {
      event.target.src = '/images/image-error.png'; // 替换为默认的错误图片
      event.target.classList.add('image-load-error');
    };
    
    // Nueva función para buscar y asignar avatar para un amigo específico
    const fetchAndAssignAvatarForFriend = async (friendIdToFetch) => {
      try {
        const response = await axios.get(
          `${window.apiBaseUrl}/api/user/${friendIdToFetch}`,
          { headers: { 'UserId': userId.value.toString() } }
        );
        if (response.data && response.data.code === 200 && response.data.data) {
          const userData = response.data.data;
          let avatarUrl = userData.avatar || '';
          if (avatarUrl && !avatarUrl.startsWith('http')) {
            avatarUrl = avatarUrl.startsWith('/') ? avatarUrl : `/${avatarUrl}`;
            avatarUrl = `${window.apiBaseUrl}${avatarUrl}`;
          }

          const friendInList = friends.value.find(f => f.userId === friendIdToFetch);
          if (friendInList) {
            friendInList.avatar = avatarUrl;
            friendInList.status = userData.status || 'offline';
          }
          // Actualizar también filteredFriends si es una copia reactiva separada
          const friendInFilteredList = filteredFriends.value.find(f => f.userId === friendIdToFetch);
           if (friendInFilteredList) {
            friendInFilteredList.avatar = avatarUrl;
            friendInFilteredList.status = userData.status || 'offline';
          }
        }
      } catch (error) {
        console.error(`加载好友 ${friendIdToFetch} 的头像失败:`, error);
      }
    };

    onMounted(async () => {
      await loadFriends(); // Carga inicial de amigos

      // Después de la carga inicial, iterar y asegurar que todos los avatares se obtengan si faltan
      const avatarFetchPromises = friends.value
        .filter(friend => !friend.avatar) // Solo buscar para aquellos que realmente les falta un avatar
        .map(friend => fetchAndAssignAvatarForFriend(friend.userId));
      
      if (avatarFetchPromises.length > 0) {
        await Promise.all(avatarFetchPromises);
        console.log('所有缺失的好友头像已尝试加载完成。');
      }

      if (currentFriendId.value) {
        // Asegurar que la información del amigo actual también se cargue/actualice
        const currentFriendData = friends.value.find(f => f.userId === currentFriendId.value);
        if (currentFriendData) {
             selectFriend(currentFriendData); // Esto llamará a loadFriendInfo si es necesario o actualizará la UI
        } else {
            // Si el amigo actual no está en la lista (podría suceder si el ID viene de la URL y la lista está vacía/desactualizada)
            await loadFriendInfo(currentFriendId.value);
        }
      }
      
      await setupSignalR();
      
      if (currentFriendId.value && connection.value && isConnected.value) {
        try {
            await connection.value.invoke('JoinPrivateChat', currentFriendId.value);
            await loadChatHistory();
        } catch (error) {
            console.error("onMounted: 加入私聊或加载历史记录失败", error)
        }
      }
      
      document.addEventListener('click', handleClickOutside);
    });
    
    onUnmounted(async () => {
      if (connection.value) {
        try {
          await connection.value.stop();
          console.log('SignalR连接已断开');
        } catch (err) {
          console.error('断开SignalR连接失败:', err);
        }
      }
      document.removeEventListener('click', handleClickOutside);
    });
    
    return {
      userId,
      username,
      userAvatar,
      currentFriendId,
      friendInfo,
      friends,
      filteredFriends,
      friendSearch,
      messages,
      newMessage,
      chatContent,
      messageInput,
      isConnected,
      showEmojiPanel,
      emojis,
      fileInput,
      imageInput,
      previewImageUrl,
      showingUserCard,
      selectedUserId,
      friendsList,
      loadFriendsList,
      isUserFriend,
      showUserCard,
      closeUserCard,
      handleFriendRequestSent,
      getFullImageUrl,
      handleImageError,
      fetchAndAssignAvatarForFriend,
      loadFriendInfo,
      setupSignalR,
      loadChatHistory,
      handleClickOutside,
      showNotification,
      handleFriendSearch,
      selectFriend,
      sendMessage,
      insertEmoji,
      toggleEmojiPanel,
      addNewLine,
      formatDate,
      formatTime,
      formatFileSize,
      shouldShowDateSeparator,
      scrollToBottom,
      goBack,
      triggerFileInput,
      handleFileUpload,
      downloadFile,
      previewImage,
      closeImagePreview,
      triggerImageUpload,
      handleImageUpload,
      formatMessageUrl,
    };
  }
};
</script>

<style scoped>
.private-chat-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  width: 100%;
  background-color: #f5f5f5;
}

.chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  background-color: #fff;
  border-bottom: 1px solid #e1e1e1;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  z-index: 10;
}

.chat-info {
  display: flex;
  align-items: center;
}

.avatar-container {
  position: relative;
  margin-right: 15px;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: #1890ff;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 18px;
  overflow: hidden;
}

.avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.status-indicator {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 2px solid white;
}

.status-indicator.online {
  background-color: #52c41a;
}

.status-indicator.offline {
  background-color: #d9d9d9;
}

.user-details {
  display: flex;
  flex-direction: column;
}

.username {
  font-size: 16px;
  font-weight: bold;
  color: #333;
}

.status-text {
  font-size: 12px;
  color: #888;
}

.signature {
  font-size: 12px;
  color: #888;
  margin-top: 2px;
}

.header-actions {
  display: flex;
  align-items: center;
}

.action-button {
  background: none;
  border: none;
  color: #666;
  font-size: 18px;
  cursor: pointer;
  padding: 5px;
  margin-left: 10px;
  transition: color 0.3s;
}

.action-button:hover {
  color: #1890ff;
}

.leave-button {
  background-color: #f5222d;
  color: white;
  border: none;
  border-radius: 4px;
  padding: 6px 12px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.3s;
}

.leave-button:hover {
  background-color: #ff4d4f;
}

/* 主内容区 */
.chat-main {
  display: flex;
  flex: 1;
  overflow: hidden;
}

/* 侧边栏样式 */
.sidebar {
  width: 260px;
  background-color: #fff;
  border-right: 1px solid #e8e8e8;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sidebar-content {
  display: flex;
  flex-direction: column;
  flex: 1;
  overflow-y: auto;
}

.sidebar-header {
  padding: 15px;
  border-bottom: 1px solid #e8e8e8;
  display: flex;
  flex-direction: column;
}

.sidebar-header h2 {
  margin: 0 0 10px 0;
  font-size: 16px;
  color: #333;
}

.search-box {
  margin-top: 5px;
}

.search-box input {
  width: 100%;
  padding: 8px 10px;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.3s;
}

.search-box input:focus {
  border-color: #1890ff;
  outline: none;
}

.friends-list {
  padding: 10px;
}

.friend-item {
  display: flex;
  align-items: center;
  padding: 10px;
  border-radius: 4px;
  cursor: pointer;
  transition: background-color 0.3s;
  margin-bottom: 5px;
}

.friend-item:hover {
  background-color: #f0f0f0;
}

.friend-item.active {
  background-color: #e6f7ff;
}

.friend-avatar {
  position: relative;
  margin-right: 10px;
}

.friend-avatar img, .friend-avatar .default-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: #1890ff;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 16px;
  overflow: hidden;
}

.friend-avatar img {
  object-fit: cover;
}

.friend-details {
  flex: 1;
  overflow: hidden;
}

.friend-name {
  font-size: 14px;
  font-weight: 500;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.friend-status {
  font-size: 12px;
  color: #888;
}

.empty-friends {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 30px 0;
  color: #999;
}

.friends-icon {
  font-size: 32px;
  margin-bottom: 10px;
}

/* 聊天内容区域 */
.chat-content {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
  background-color: #f5f5f5;
}

.empty-chat-message {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #999;
}

.empty-chat-message i {
  font-size: 48px;
  margin-bottom: 15px;
}

.messages-wrapper {
  padding: 15px 20px; /* 调整内边距 */
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 18px; /* 消息间的垂直间距 */
}

.date-separator {
  text-align: center;
  margin: 10px 0;
  color: #888;
  font-size: 12px;
  width: 100%;
}

.message-item {
  display: flex;
  gap: 10px; /* 头像和消息内容之间的间距 */
  width: 100%;
  align-items: flex-start; /* 垂直方向上，头像和消息内容顶部对齐 */
}

.message-item.self-message {
  flex-direction: row-reverse; /* 自己消息：头像在右，内容在左（视觉上）*/
}

.message-avatar-container {
  flex-shrink: 0; /* 防止头像被压缩 */
}

.avatar-display {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #ccc; /* 默认头像背景 */
  font-weight: bold;
}
.avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.avatar-default {
  color: white;
  font-size: 16px;
}
.message-item.self-message .avatar-display {
   background-color: #1890ff; /* 自己头像的默认背景 */
}
.message-item:not(.self-message) .avatar-display {
   background-color: #888; /* 对方头像的默认背景 */
}

.message-content-area {
  display: flex;
  flex-direction: column;
  max-width: 70%; /* 限制消息内容区域的最大宽度 */
  min-width: 0; /* 防止flex item在max-width下无法正确收缩 */
}

.message-item.self-message .message-content-area {
  align-items: flex-end; /* 自己消息：内部元素（头部、气泡）靠右对齐 */
}

.message-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 5px; /* 头部和气泡之间的间距 */
  font-size: 12px;
  color: #666;
  line-height: 1;
}
.message-item.self-message .message-header {
  justify-content: flex-end; /* 自己的消息头部内容（时间）也靠右 */
}

.message-sender {
  font-weight: 500;
  color: #555;
}

.message-time {
  color: #999;
}

/* 消息气泡 */
.message-body {
  /* 气泡本身不需要特定的flex属性，其对齐由父级 .message-content-area 控制 */
}

.message-text {
  padding: 10px 15px;
  font-size: 14px;
  line-height: 1.5;
  word-break: break-all;
  box-shadow: 0 1px 2px rgba(0,0,0,0.05);
}

/* 对方消息气泡样式 */
.message-item:not(.self-message) .message-text {
  background-color: #ffffff;
  color: #333;
  border-radius: 4px 16px 16px 16px; /* 左上角尖一点，其他圆角 */
}

/* 自己消息气泡样式 */
.message-item.self-message .message-text {
  background-color: #1890ff;
  color: #ffffff;
  border-radius: 16px 4px 16px 16px; /* 右上角尖一点，其他圆角 */
}

.message-image img {
  max-width: 100%; /* 图片宽度不超过气泡 */
  max-height: 220px;
  border-radius: 8px;
  cursor: pointer;
  display: block;
  background-color: #e9ecef; /* 图片加载时的占位背景 */
}
.image-info {
  font-size: 11px;
  color: #888;
  margin-top: 4px;
}
.message-item.self-message .image-info {
  text-align: right;
}

.message-file {
  display: inline-flex; /* 使其包裹内容 */
  align-items: center;
  padding: 8px 12px;
  border-radius: 8px;
  cursor: pointer;
  gap: 8px;
  max-width: 100%; /* 不超过父容器 */
}
.message-item:not(.self-message) .message-file {
  background-color: #f8f9fa;
  border: 1px solid #e9ecef;
}
.message-item.self-message .message-file {
  background-color: #e3f2fd; /* 自己的文件消息用淡蓝色 */
  border: 1px solid #bbdefb;
}

.file-icon i {
  font-size: 20px;
  color: #007bff;
}
.file-info {
  overflow: hidden; /* 防止文件名过长破坏布局 */
}
.file-name {
  font-size: 13px;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.file-size {
  font-size: 11px;
  color: #777;
}
.download-icon i {
  font-size: 16px;
  color: #007bff;
  margin-left: 5px;
}

.message-emoji {
  font-size: 26px; /* 表情大小 */
  padding: 2px 5px; /* 给点内边距，使其像气泡 */
  line-height: 1;
  display: inline-block; /* 使其像文本一样流动但可以设置padding */
}
/* 可选：给emoji也加上类似气泡的背景 */
.message-item:not(.self-message) .message-emoji {
  /* background-color: #fff; */
  /* border-radius: 10px; */
}
.message-item.self-message .message-emoji {
   /* background-color: #1890ff; */
   /* color: #fff; */ /* 如果背景是深色 */
   /* border-radius: 10px; */
}

/* 底部输入区 */
.chat-footer {
  padding: 15px;
  background-color: #fff;
  border-top: 1px solid #e8e8e8;
  display: flex;
  align-items: center;
}

.toolbar {
  display: flex;
  margin-right: 10px;
}

.tool-button {
  font-size: 18px;
  color: #666;
  margin-right: 10px;
  cursor: pointer;
  transition: color 0.3s;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background-color: #f5f5f5;
}

.tool-button:hover {
  color: #1890ff;
  background-color: #e6f7ff;
}

.tool-button i {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
}

.emoji-panel {
  position: absolute;
  bottom: 80px;
  left: 20px;
  width: 300px;
  height: 200px;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  padding: 10px;
  overflow-y: auto;
  display: grid;
  grid-template-columns: repeat(8, 1fr);
  gap: 5px;
  z-index: 100;
}

.emoji-item {
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  cursor: pointer;
  padding: 5px;
  border-radius: 4px;
  transition: background-color 0.3s;
}

.emoji-item:hover {
  background-color: #f0f0f0;
}

textarea {
  flex: 1;
  resize: none;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  padding: 10px;
  font-size: 14px;
  min-height: 60px;
  max-height: 120px;
  transition: border-color 0.3s;
}

textarea:focus {
  outline: none;
  border-color: #1890ff;
}

button {
  margin-left: 10px;
  background-color: #1890ff;
  color: white;
  border: none;
  border-radius: 4px;
  padding: 8px 16px;
  font-size: 14px;
  cursor: pointer;
  transition: background-color 0.3s;
}

button:hover {
  background-color: #40a9ff;
}

button:disabled {
  background-color: #d9d9d9;
  cursor: not-allowed;
}

/* 响应式布局 */
@media (max-width: 768px) {
  .sidebar {
    display: none;
  }
  
  .chat-content {
    padding: 10px;
  }
  
  .message-body {
    max-width: 85%;
  }
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
  top: -40px;
  right: 0;
  background: none;
  border: none;
  color: white;
  font-size: 30px;
  cursor: pointer;
  padding: 5px;
  line-height: 1;
}

.close-preview:hover {
  opacity: 0.8;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}
</style>
