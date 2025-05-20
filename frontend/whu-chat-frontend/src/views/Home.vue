<template>
  <div class="home-container">
    <!-- 侧边导航栏 -->
    <div class="sidebar">
      <!-- 用户信息 -->
      <div class="user-profile">
        <div class="avatar-container">
          <img v-if="userAvatar" :src="userAvatar" :alt="username" class="user-avatar">
          <div v-else class="user-avatar-placeholder">{{ username.charAt(0).toUpperCase() }}</div>
          <div class="online-indicator"></div>
        </div>
        <div class="user-info">
          <h3 class="username">{{ username }}</h3>
          <p class="status">在线</p>
          <p v-if="userSignature" class="user-signature">{{ userSignature }}</p>
        </div>
        <div class="user-menu" @click="toggleUserMenu">
          <i class="fa-solid fa-ellipsis-vertical"></i>
          <div class="dropdown-menu" v-if="showUserMenu">
            <div class="menu-item" @click="showProfileModal = true">
              <i class="fa-solid fa-user"></i>
              <span>个人资料</span>
            </div>
            <div class="menu-item" @click="showSettingsModal = true">
              <i class="fa-solid fa-gear"></i>
              <span>设置</span>
            </div>
            <div class="menu-item logout" @click="logout">
              <i class="fa-solid fa-right-from-bracket"></i>
              <span>退出登录</span>
            </div>
          </div>
        </div>
      </div>

      <!-- 导航菜单 -->
      <div class="nav-menu">
        <div class="search-box">
          <i class="fa-solid fa-search"></i>
          <input type="text" placeholder="搜索..." v-model="searchQuery">
          <i class="fa-solid fa-times" v-if="searchQuery" @click="searchQuery = ''"></i>
        </div>
        
        <div class="menu-section">
          <div class="section-header">
            <i class="fa-solid fa-compass"></i>
            <span>发现</span>
          </div>
          <div class="nav-item" 
               @click="activeSection = 'chatrooms'" 
               :class="{ active: activeSection === 'chatrooms' }">
            <i class="fa-solid fa-comments"></i>
            <span>公共聊天室</span>
            <div class="notification-badge" v-if="chatroomNotifications > 0">{{ chatroomNotifications }}</div>
          </div>
          <div class="nav-item" 
               @click="navigateToAIChat()" 
               :class="{ active: false }">
            <i class="fa-solid fa-robot"></i>
            <span>AI咨询</span>
          </div>
          <div class="nav-item" 
               @click="navigateToForums()" 
               :class="{ active: false }">
            <i class="fa-solid fa-comment-dots"></i>
            <span>讨论区</span>
          </div>
        </div>
        
        <div class="menu-section">
          <div class="section-header">
            <i class="fa-solid fa-user-group"></i>
            <span>联系人</span>
            <div class="add-button" @click="showAddFriendModal = true">
              <i class="fa-solid fa-plus"></i>
            </div>
          </div>
          <div class="nav-item" 
               @click="activeSection = 'friends'" 
               :class="{ active: activeSection === 'friends' }">
            <i class="fa-solid fa-user-friends"></i>
            <span>好友</span>
            <div class="notification-badge" v-if="friendNotifications > 0">{{ friendNotifications }}</div>
          </div>
          <div class="expandable-section">
            <div class="section-toggle" @click="showOnlineFriends = !showOnlineFriends">
              <i :class="showOnlineFriends ? 'fa-solid fa-chevron-down' : 'fa-solid fa-chevron-right'"></i>
              <span>好友列表 ({{ friendsList.length }})</span>
            </div>
            <transition name="slide-fade">
              <div class="friend-list" v-if="showOnlineFriends">
                <div v-for="friend in friendsList" :key="friend.id" 
                     class="friend-item" @click="openPrivateChat(friend)">
                  <div class="friend-avatar">
                    <img v-if="friend.avatar" :src="friend.avatar" :alt="friend.username" class="avatar-image">
                    <div v-else class="avatar-placeholder">{{ friend.username.charAt(0).toUpperCase() }}</div>
                    <div class="friend-status" :class="friend.status"></div>
                  </div>
                  <div class="friend-name">{{ friend.username }}</div>
                </div>
                <div class="empty-list" v-if="friendsList.length === 0">
                  <i class="fa-solid fa-user-slash"></i>
                  <span>暂无好友</span>
                </div>
              </div>
            </transition>
          </div>
        </div>
        
        <div class="menu-section">
          <div class="section-header">
            <i class="fa-solid fa-users"></i>
            <span>群组</span>
            <div class="add-button" @click="showCreateGroupModal = true">
              <i class="fa-solid fa-plus"></i>
            </div>
          </div>
          <div class="nav-item" 
               @click="activeSection = 'groups'" 
               :class="{ active: activeSection === 'groups' }">
            <i class="fa-solid fa-layer-group"></i>
            <span>群组管理</span>
          </div>
          <div class="nav-item" 
               @click="navigateToGroupChat()" 
               :class="{ active: false }">
            <i class="fa-solid fa-comments"></i>
            <span>群组聊天</span>
          </div>
        </div>

        <div class="nav-item" @click="fetchNotifications" style="cursor:pointer;">
          <i class="fa-solid fa-bell"></i>
          <span>系统通知</span>
          <div class="notification-badge" v-if="unreadNotifications > 0">{{ unreadNotifications }}</div>
        </div>

        <div class="nav-item" @click="navigateToMemberBenefits()" style="cursor:pointer;">
          <i class="fa-solid fa-crown"></i>
          <span>升级权益</span>
        </div>
      </div>
    </div>

    <!-- 主内容区域 -->
    <div class="main-content">
      <!-- 聊天室部分 -->
      <div v-if="activeSection === 'chatrooms'" class="content-section chatrooms-section">
        <div class="section-title">
          <h2><i class="fa-solid fa-comments"></i> 公共聊天室</h2>
          <p>加入公共聊天室与其他用户交流</p>
        </div>
        
        <div class="rooms-grid">
          <div v-for="room in chatRooms" :key="room.id" 
               class="room-card" @click="enterChatRoom(room)">
            <div class="room-header">
              <div class="room-icon">
                <i :class="room.icon || 'fa-solid fa-comments'"></i>
              </div>
              <div class="room-info">
                <h3>{{ room.name }}</h3>
                <p class="room-stats">
                  <span><i class="fa-solid fa-user"></i> {{ room.onlineCount }} 在线</span>
                  <span><i class="fa-solid fa-message"></i> {{ room.messageCount }} 消息</span>
                </p>
              </div>
            </div>
            <p class="room-description">{{ room.description }}</p>
            <div class="room-footer">
              <span class="join-button">
                <i class="fa-solid fa-right-to-bracket"></i> 进入聊天室
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- 好友部分 -->
      <div v-if="activeSection === 'friends'" class="content-section friends-section">
        <div class="section-title">
          <h2><i class="fa-solid fa-user-friends"></i> 我的好友</h2>
          <p>与好友私聊交流</p>
        </div>
        
        <div class="friends-container">
          <div class="friends-search">
            <input type="text" placeholder="搜索好友..." v-model="friendSearch">
            <button class="add-friend-btn" @click="showAddFriendModal = true">
              <i class="fa-solid fa-user-plus"></i> 添加好友
            </button>
          </div>
          
          <div class="friends-list">
            <div v-for="friend in filteredFriends" :key="friend.id" 
                 class="friend-card" @click="openPrivateChat(friend)">
              <div class="friend-avatar">
                <img v-if="friend.avatar" :src="friend.avatar" :alt="friend.username" class="avatar-image">
                <div v-else class="avatar-placeholder">{{ friend.username.charAt(0).toUpperCase() }}</div>
                <div class="friend-status" :class="friend.status"></div>
              </div>
              <div class="friend-details">
                <h3>{{ friend.username }}</h3>
                <p class="friend-status-text">{{ friend.status === 'online' ? '在线' : '离线' }}</p>
                <p v-if="friend.signature" class="friend-signature">{{ friend.signature }}</p>
              </div>
              <div class="friend-actions">
                <button class="action-button chat">
                  <i class="fa-solid fa-comment"></i>
                </button>
                <button class="action-button delete" @click.stop="confirmDeleteFriend(friend)">
                  <i class="fa-solid fa-user-minus"></i>
                </button>
              </div>
            </div>
            
            <div class="empty-friends" v-if="filteredFriends.length === 0">
              <div class="empty-icon">
                <i class="fa-solid fa-user-plus"></i>
              </div>
              <h3>没有好友</h3>
              <p>添加好友开始私聊</p>
              <button class="add-friend-btn" @click="showAddFriendModal = true">
                添加好友
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- 群组部分 -->
      <div v-if="activeSection === 'groups'" class="content-section groups-section">
        <div class="section-title">
          <h2><i class="fa-solid fa-users"></i> 群组管理</h2>
          <p>与群组成员交流讨论</p>
        </div>
        
        <div class="groups-container">
          <div class="groups-search">
            <input type="text" placeholder="搜索群组..." v-model="groupSearch">
            <button class="create-group-btn" @click="showCreateGroupModal = true">
              <i class="fa-solid fa-plus"></i> 创建群组
            </button>
          </div>
          
          <div class="groups-list">
            <div v-for="group in filteredGroups" :key="group.groupId" 
                 class="group-card" @click="selectGroup(group)"
                 :class="{ active: selectedGroup?.groupId === group.groupId }">
              <div class="group-avatar">
                <div class="avatar-placeholder">{{ group.groupName.charAt(0).toUpperCase() }}</div>
              </div>
              <div class="group-details">
                <h3>{{ group.groupName }}</h3>
                <p class="group-stats">
                  <span><i class="fa-solid fa-clock"></i> {{ new Date(group.updateTime).toLocaleString() }}</span>
                </p>
              </div>
            </div>
            
            <div class="empty-groups" v-if="filteredGroups.length === 0">
              <div class="empty-icon">
                <i class="fa-solid fa-users"></i>
              </div>
              <h3>没有群组</h3>
              <p>创建或加入群组开始交流</p>
              <button class="create-group-btn" @click="showCreateGroupModal = true">
                创建群组
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- 群组详情面板 -->
      <div class="group-detail-panel" v-if="selectedGroup">
        <div class="panel-header">
          <div class="header-left">
            <h3>{{ selectedGroup.groupName }}</h3>
            <p class="group-description">{{ selectedGroup.description }}</p>
          </div>
          <button class="close-button" @click="selectedGroup = null">
            <i class="fa-solid fa-times"></i>
          </button>
        </div>
        
        <div class="panel-content">
          <div class="group-info">
            <div class="info-item">
              <i class="fa-solid fa-user"></i>
              <span>{{ selectedGroup.memberCount }} 成员</span>
            </div>
            <div class="info-item">
              <i class="fa-solid fa-calendar"></i>
              <span>创建于 {{ new Date(selectedGroup.createTime).toLocaleString() }}</span>
            </div>
          </div>

          <!-- 添加成员表单 -->
          <div class="add-member-section" v-if="showAddUserModal">
            <div class="section-header">
              <h4>添加新成员</h4>
              <button class="close-button" @click="showAddUserModal = false">
                <i class="fa-solid fa-times"></i>
              </button>
            </div>
            <div class="add-member-form">
              <div class="form-group">
                <label for="userName">用户名</label>
                <input type="text" id="userName" v-model="newUserName" placeholder="输入用户名...">
              </div>
              <div class="form-actions">
                <button class="action-button cancel" @click="showAddUserModal = false">
                  <i class="fa-solid fa-times"></i> 取消
                </button>
                <button class="action-button submit" @click="addUserToGroup">
                  <i class="fa-solid fa-user-plus"></i> 添加
                </button>
              </div>
            </div>
          </div>

          <div class="member-section">
            <div class="section-header">
              <h4>群组成员</h4>
              <div class="member-search-box">
                <i class="fa-solid fa-search"></i>
                <input 
                  type="text" 
                  v-model="memberSearch" 
                  placeholder="搜索成员..." 
                >
              </div>
            </div>
            <div class="members">
              <div v-for="member in filteredMembers" :key="member.userId" class="member-item">
                <div class="member-avatar">
                  <img v-if="member.avatar" :src="member.avatar" :alt="member.username" class="avatar-image">
                  <div v-else class="avatar-placeholder">{{ member.username.charAt(0).toUpperCase() }}</div>
                </div>
                <div class="member-info">
                  <span class="member-name">{{ member.username }}</span>
                  <span class="member-role" v-if="member.role === 'master'">群主</span>
                  <span class="member-role admin" v-else-if="member.role === 'admin'">管理员</span>
                </div>
                <div class="member-actions" v-if="member.userId !== selectedGroup.creatorId">
                  <button class="action-button toggle-admin" @click="toggleAdminRole(member)" v-if="isCurrentUserAdmin">
                    <i class="fa-solid" :class="member.role === 'admin' ? 'fa-user-minus' : 'fa-user-shield'"></i>
                  </button>
                  <button class="action-button remove" @click="removeUserFromGroup(member)" v-if="isCurrentUserAdmin">
                    <i class="fa-solid fa-user-minus"></i>
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div class="action-buttons">
            <button class="action-button add" @click="showAddUserModal = true">
              <i class="fa-solid fa-user-plus"></i> 添加成员
            </button>
            <button class="action-button delete" @click="confirmDeleteGroup">
              <i class="fa-solid fa-trash"></i> 删除群组
            </button>
          </div>
        </div>
      </div>

      <!-- 删除模态窗口 -->
      <div class="modal-overlay" v-if="showDeleteConfirm" @click="showDeleteConfirm = false">
        <div class="modal-content" @click.stop>
          <div class="modal-header">
            <h3>确认删除</h3>
            <button class="close-button" @click="showDeleteConfirm = false">
              <i class="fa-solid fa-times"></i>
            </button>
          </div>
          <div class="modal-body">
            <p>确定要删除这个群组吗？此操作不可恢复。</p>
            <div class="modal-actions">
              <button class="action-button cancel" @click="showDeleteConfirm = false">取消</button>
              <button class="action-button delete" @click="deleteGroup">确认删除</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 模态窗口：添加好友 -->
    <div class="modal-overlay" v-if="showAddFriendModal" @click="showAddFriendModal = false">
      <div class="modal-content add-friend-modal" @click.stop>
        <div class="modal-header">
          <h3><i class="fa-solid fa-user-plus"></i> 添加好友</h3>
          <button class="close-button" @click="showAddFriendModal = false">
            <i class="fa-solid fa-times"></i>
          </button>
        </div>
        <div class="modal-body">
          <div class="add-friend-form">
            <div class="form-group">
              <div class="search-input-wrapper">
                <i class="fa-solid fa-user search-icon"></i>
                <input 
                  type="text" 
                  id="friendUsername" 
                  v-model="friendUsername" 
                  placeholder="输入用户名搜索..."
                  class="search-input"
                >
                <div class="input-focus-border"></div>
              </div>
              <p class="input-hint">
                <i class="fa-solid fa-lightbulb"></i>
                输入对方的用户名，系统将自动搜索
              </p>
            </div>
            <div class="form-actions">
              <button class="action-button cancel" @click="showAddFriendModal = false">
                <i class="fa-solid fa-times"></i> 取消
              </button>
              <button class="action-button submit" @click="addFriend">
                <i class="fa-solid fa-user-plus"></i> 添加好友
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 模态窗口：创建群组 -->
    <div class="modal-overlay" v-if="showCreateGroupModal" @click="showCreateGroupModal = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>创建群组</h3>
          <button class="close-button" @click="showCreateGroupModal = false">
            <i class="fa-solid fa-times"></i>
          </button>
        </div>
        <div class="modal-body">
          <div class="create-group-form">
            <div class="form-group">
              <label for="groupName">群组名称</label>
              <input 
                type="text" 
                id="groupName" 
                v-model="groupName" 
                placeholder="输入群组名称..."
                class="form-input"
              >
            </div>
            <div class="form-group">
              <label for="groupDescription">群组描述</label>
              <textarea 
                id="groupDescription" 
                v-model="groupDescription" 
                placeholder="输入群组描述..."
                class="form-textarea"
              ></textarea>
            </div>
            <div class="form-actions">
              <button class="action-button cancel" @click="showCreateGroupModal = false">
                <i class="fa-solid fa-times"></i> 取消
              </button>
              <button class="action-button submit" @click="createGroup">
                <i class="fa-solid fa-plus"></i> 创建
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 系统通知模态窗口 -->
    <div v-if="showNotifications" class="notification-modal-overlay" @click="showNotifications = false">
      <div class="notification-modal-content" @click.stop>
        <div class="notification-modal-header">
          <h3>系统通知</h3>
          <button class="close-button" @click="showNotifications = false">
            <i class="fa-solid fa-times"></i>
          </button>
        </div>
        <div class="notification-modal-body">
          <ul class="notification-list">
            <li v-for="n in notifications" :key="n.notificationId" 
                class="notification-item" 
                :class="{'unread': !n.isRead, 'friend-request': n.type === 'friend_request'}">
              <div class="notification-content">
                <!-- 好友请求通知，特殊处理 -->
                <template v-if="n.type === 'friend_request'">
                  <div class="friend-request-header">
                    {{ n.content.split('\n')[0] }}
                  </div>
                  <div class="friend-request-message" v-if="n.content.includes('验证消息')">
                    {{ n.content.split('\n')[1] }}
                  </div>
                  <div class="friend-request-actions" v-if="!n.isHandled">
                    <button class="accept-button" @click.stop="acceptFriend(n.notificationId)">同意</button>
                    <button class="reject-button" @click.stop="rejectFriend(n.notificationId)">拒绝</button>
                  </div>
                  <div class="friend-request-status" v-else>
                    <span>已处理</span>
                  </div>
                </template>
                <!-- 其他类型通知 -->
                <template v-else>
                  {{ n.content }}
                </template>
              </div>
              <div class="notification-time">
                {{ formatNotificationTime(n.createdAt) }}
              </div>
            </li>
            <li v-if="notifications.length === 0" class="empty-notification">暂无通知</li>
          </ul>
        </div>
      </div>
    </div>

    <!-- 个人资料模态框 -->
    <user-profile 
      v-if="showProfileModal" 
      :visible="showProfileModal"
      @close="showProfileModal = false"
      @profile-updated="handleProfileUpdated"
    />

    <!-- 设置模态框 -->
    <user-settings 
      v-if="showSettingsModal" 
      :visible="showSettingsModal"
      @close="showSettingsModal = false"
      @settings-updated="handleSettingsUpdated"
    />
    
    <!-- 通知提示 -->
    <transition name="notification-fade">
      <div v-if="notification.show" 
           class="notification-toast" 
           :class="notification.type">
        <i :class="{
          'fa-solid fa-check-circle': notification.type === 'success',
          'fa-solid fa-exclamation-circle': notification.type === 'error',
          'fa-solid fa-info-circle': notification.type === 'info'
        }"></i>
        <span>{{ notification.message }}</span>
      </div>
    </transition>
  </div>
</template>

<script>
import { ref, computed, onMounted, watch, onBeforeUnmount } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';
import UserProfile from '@/components/UserProfile.vue';
import UserSettings from '@/components/UserSettings.vue';

export default {
  name: 'HomeView',
  components: {
    UserProfile,
    UserSettings
  },
  setup() {
    const router = useRouter();
    
    // 用户信息
    const userId = ref(localStorage.getItem('userId') || '');
    const username = ref(localStorage.getItem('username') || '');
    const userAvatar = ref(localStorage.getItem('userAvatar') || '');
    const userSignature = ref(localStorage.getItem('userSignature') || '');
    
    // UI 状态
    const activeSection = ref('chatrooms');
    const searchQuery = ref('');
    const showUserMenu = ref(false);
    const showProfileModal = ref(false);
    const showSettingsModal = ref(false);
    const showAddFriendModal = ref(false);
    const showCreateGroupModal = ref(false);
    const showOnlineFriends = ref(true);
    const showAddUserModal = ref(false);
    const showDeleteConfirm = ref(false); // 添加删除确认对话框状态
    
    // 表单数据
    const friendUsername = ref('');
    const groupId = ref('');
    const groupName = ref('');
    const groupDescription = ref('');
    
    // 搜索过滤
    const friendSearch = ref('');
    const groupSearch = ref('');
    
    // 通知计数
    const chatroomNotifications = ref(0);
    const friendNotifications = ref(0);
    const groupNotifications = ref(2);
    
    // 群组列表
    const groups = ref([]);
    
    // 聊天室列表
    const chatRooms = ref([
      {
        id: 1,
        name: '校园公共聊天室',
        description: '欢迎来到武汉大学校园公共聊天室，这里是交流分享的空间！',
        icon: 'fa-solid fa-school',
        onlineCount: 15,
        messageCount: 1024
      },
      {
        id: 2,
        name: '交友聊天室',
        description: '在这里找到志同道合的朋友，扩展你的社交圈',
        icon: 'fa-solid fa-user-group',
        onlineCount: 8,
        messageCount: 536
      },
      {
        id: 3,
        name: '学术交流厅',
        description: '讨论学术问题，分享研究心得和学习资源',
        icon: 'fa-solid fa-graduation-cap',
        onlineCount: 12,
        messageCount: 789
      },
      {
        id: 4,
        name: '游戏玩家俱乐部',
        description: '讨论热门游戏，组队开黑，分享游戏攻略',
        icon: 'fa-solid fa-gamepad',
        onlineCount: 20,
        messageCount: 1256
      },
      {
        id: 5,
        name: '求职交流区',
        description: '分享求职经验，讨论职业规划，互通招聘信息',
        icon: 'fa-solid fa-briefcase',
        onlineCount: 10,
        messageCount: 678
      }
    ]);
    
    // 好友列表
    const friendsList = ref([]);
    
    // 最近聊天列表
    const recentChats = ref([]);
    
    // 群组详情相关
    const selectedGroup = ref(null);
    const groupMembers = ref([]);
    const memberSearch = ref('');
    const newUserName = ref('');
    
    // 系统通知相关
    const notifications = ref([]);
    const showNotifications = ref(false);
    const unreadNotifications = ref(0);
    
    // 在 setup() 函数中添加更新未读通知数量的函数
    const updateUnreadNotifications = async () => {
      try {
        const res = await axios.get(`/api/notification/user/${userId.value}`);
        // 只计算未处理的通知数量
        unreadNotifications.value = res.data.filter(n => !n.isHandled).length;
      } catch (error) {
        console.error('获取未读通知数量失败:', error);
      }
    };
    
    // 获取群组列表
    const fetchGroups = async () => {
      try {
        const response = await axios.get(`${window.apiBaseUrl}/api/Group/user/${userId.value}`);
        if (response.data && response.data.code === 200) {
          groups.value = response.data.data;
        } else {
          throw new Error(response.data?.msg || '获取群组列表失败');
        }
      } catch (error) {
        console.error('获取群组列表失败:', error);
        showNotification('获取群组列表失败: ' + error.message, 'error');
      }
    };
    
    // 处理群组搜索
    const handleGroupSearch = async () => {
      try {
        if (!groupSearch.value.trim()) {
          // 如果搜索框为空，获取所有群组
          await fetchGroups();
        } else {
          // 调用搜索API
          const response = await axios.get(`${window.apiBaseUrl}/api/Group/search?groupName=${encodeURIComponent(groupSearch.value)}&userId=${userId.value}`);
          if (response.data && response.data.code === 200) {
            groups.value = response.data.data;
          } else {
            throw new Error(response.data?.msg || '搜索群组失败');
          }
        }
      } catch (error) {
        console.error('搜索群组失败:', error);
        showNotification('搜索群组失败: ' + error.message, 'error');
      }
    };

    // 添加防抖函数
    const debounce = (fn, delay) => {
      let timer = null;
      return function (...args) {
        if (timer) clearTimeout(timer);
        timer = setTimeout(() => {
          fn.apply(this, args);
        }, delay);
      };
    };

    // 使用防抖处理搜索
    const debouncedGroupSearch = debounce(handleGroupSearch, 300);

    // 监听搜索输入
    watch(groupSearch, () => {
      debouncedGroupSearch();
    });

    // 修改计算属性
    const filteredGroups = computed(() => {
      return groups.value;
    });
    
    // 加载好友列表
    const fetchFriends = async () => {
      try {
        const apiBaseUrl = window.apiBaseUrl || '';
        const friendsResponse = await axios.get(`${apiBaseUrl}/api/user/${userId.value}/friends`);
        if (friendsResponse.data.code === 200) {
          friendsList.value = friendsResponse.data.data.map(friend => {
            let avatarUrl = friend.avatar || '';
            if (avatarUrl) {
              if (!avatarUrl.startsWith('http')) {
                avatarUrl = avatarUrl.startsWith('/') ? avatarUrl : `/${avatarUrl}`;
                avatarUrl = `${apiBaseUrl}${avatarUrl}`;
              }
              // 添加时间戳防止缓存
              avatarUrl = `${avatarUrl}?t=${new Date().getTime()}`;
            }
            
            return {
              id: friend.userId || friend.id,
              userId: friend.userId || friend.id,
              username: friend.username,
              avatar: avatarUrl,
              status: friend.status || 'offline',
              signature: friend.signature || ''
            };
          });
          
          console.log('获取的好友列表:', friendsList.value);
          await updateFriendsOnlineStatus();
        }
      } catch (error) {
        console.error('获取好友列表失败:', error);
      }
    };
    
    // 更新好友在线状态
    const updateFriendsOnlineStatus = async () => {
      try {
        // 获取所有在线用户
        const apiBaseUrl = window.apiBaseUrl || ''; // 使用全局配置的API URL或空字符串
        const response = await axios.get(`${apiBaseUrl}/api/user/online`);
        if (response.data && response.data.code === 200) {
          const onlineUserIds = response.data.data;
          
          // 更新每个好友的在线状态
          friendsList.value.forEach(friend => {
            friend.status = onlineUserIds.includes(friend.id) ? 'online' : 'offline';
          });
        }
      } catch (error) {
        console.error('获取在线用户状态失败:', error);
      }
    };
    
    // 定时更新在线状态
    let statusUpdateInterval = null;
    
    const forumCategories = ref([
      {
        id: 301,
        name: '课程讨论',
        description: '分享课程心得，解答学习疑问',
        icon: 'fa-solid fa-book',
        postCount: 128
      },
      {
        id: 302,
        name: '校园生活',
        description: '讨论校园生活、食堂、宿舍等话题',
        icon: 'fa-solid fa-utensils',
        postCount: 85
      },
      {
        id: 303,
        name: '活动公告',
        description: '发布校园各类活动信息',
        icon: 'fa-solid fa-bullhorn',
        postCount: 42
      },
      {
        id: 304,
        name: '就业实习',
        description: '分享实习经验，就业信息',
        icon: 'fa-solid fa-briefcase',
        postCount: 67
      }
    ]);
    
    // 计算属性
    const onlineFriends = computed(() => {
      return friendsList.value; // 返回所有好友，不进行在线过滤
    });
    
    const filteredFriends = computed(() => {
      if (!friendSearch.value) return friendsList.value;
      return friendsList.value.filter(friend => 
        friend.username.toLowerCase().includes(friendSearch.value.toLowerCase())
      );
    });
    
    const filteredMembers = computed(() => {
      if (!memberSearch.value) return groupMembers.value;
      return groupMembers.value.filter(member => 
        member.username.toLowerCase().includes(memberSearch.value.toLowerCase())
      );
    });
    
    // 方法
    const toggleUserMenu = () => {
      showUserMenu.value = !showUserMenu.value;
    };
    
    const enterChatRoom = (room) => {
      localStorage.setItem('currentRoomId', room.id);
      localStorage.setItem('currentRoomName', room.name);
      router.push(`/chatroom/${room.id}`);
    };
    
    const openPrivateChat = (friend) => {
      // 保存当前聊天对象的信息到localStorage
      localStorage.setItem('currentChatFriend', JSON.stringify({
        id: friend.id,
        username: friend.username,
        avatar: friend.avatar,
        status: friend.status,
        signature: friend.signature
      }));
      router.push(`/private-chat/${friend.id}`);
    };
    
    const openGroupChat = (group) => {
      localStorage.setItem('currentGroupId', group.groupId);
      localStorage.setItem('currentGroupName', group.groupName);
      router.push(`/groupchat/${group.groupId}`);
    };
    
    const addFriend = async () => {
      if (!friendUsername.value) {
        showNotification('请输入用户名', 'error');
        return;
      }
      
      try {
        const response = await axios.post('/api/notification/friend-request', {
          targetUsername: friendUsername.value,
          requesterUsername: username.value
        });
        
        if (response.data && response.data.msg) {
          showNotification(response.data.msg, 'success');
          friendUsername.value = '';
          showAddFriendModal.value = false;
        }
      } catch (error) {
        console.error('添加好友失败:', error);
        if (error.response) {
          // 服务器返回了错误响应
          showNotification('添加好友失败: ' + (error.response.data?.msg || '服务器错误'), 'error');
        } else if (error.request) {
          // 请求发送失败
          showNotification('添加好友失败: 无法连接到服务器', 'error');
        } else {
          // 其他错误
          showNotification('添加好友失败: ' + error.message, 'error');
        }
      }
    };
    
    const createGroup = async () => {
      if (!groupName.value) {
        alert('群组名称不能为空');
        return;
      }
      
      try {
        const groupData = {
          groupName: groupName.value,
          description: groupDescription.value || '',
          creatorId: parseInt(userId.value),
          memberCount: 1
        };

        const response = await axios.post('/api/group/create', groupData);
        
        if (response.data.code === 200) {
          // 创建成功后清空输入并关闭模态窗口
          groupName.value = '';
          groupDescription.value = '';
          showCreateGroupModal.value = false;
          
          // 刷新群组列表
          await fetchGroups();
          
          alert('群组创建成功');
        } else {
          alert('创建群组失败: ' + response.data.msg);
        }
      } catch (error) {
        console.error('创建群组失败:', error);
        alert('创建群组失败: ' + (error.response?.data?.msg || error.message));
      }
    };
    
    const logout = () => {
      localStorage.removeItem('token');
      localStorage.removeItem('userId');
      localStorage.removeItem('username');
      localStorage.removeItem('userAvatar');
      localStorage.removeItem('userSignature');
      router.push('/login');
    };
    
    const navigateToAIChat = () => {
      const userId = localStorage.getItem('userId');
      const username = localStorage.getItem('username');
      
      if (!userId || !username) {
        alert('登录信息已失效，请重新登录');
        router.push('/login');
        return;
      }
      
      router.push({
        path: '/chat',
        query: {
          userId: userId,
          username: username
        }
      });
    };
    
    const navigateToGroupChat = () => {
      router.push('/groupchat');
    };
    
    const navigateToForums = () => {
      router.push('/discussion');
    };
    
    const navigateToMemberBenefits = () => {
      router.push('/member-benefits');
    };
    
    const selectGroup = async (group) => {
      try {
        console.log('选择的群组数据:', group);
        
        // 获取群组详情
        const detailResponse = await axios.get(`/api/group/${group.groupId}`);
        if (detailResponse.data.code === 200) {
          console.log('群组详情数据:', detailResponse.data.data);
          selectedGroup.value = detailResponse.data.data;
        }
        
        // 获取群组成员
        const membersResponse = await axios.get(`/api/group/${group.groupId}/users`);
        if (membersResponse.data.code === 200) {
          console.log('群组成员原始数据:', membersResponse.data.data);
          groupMembers.value = membersResponse.data.data.map(member => ({
            ...member,
            userId: member.userId,
            username: member.username,
            avatar: member.avatar,
            role: member.role,
            joinTime: member.joinTime,
            lastActive: member.lastActive
          }));
          console.log('处理后的成员数据:', groupMembers.value);
          console.log('当前用户权限:', isCurrentUserAdmin.value);
        }
      } catch (error) {
        console.error('获取群组详情失败:', error);
        alert('获取群组详情失败: ' + (error.response?.data?.msg || error.message));
      }
    };
    
    const confirmDeleteGroup = () => {
      if (confirm('确定要删除这个群组吗？此操作不可恢复。')) {
        deleteGroup();
      }
    };
    
    const deleteGroup = async () => {
      if (!selectedGroup.value) return;
      
      try {
        const response = await axios.delete(`/api/group/${selectedGroup.value.groupId}`, {
          params: {
            operatorUserId: parseInt(userId.value)
          }
        });
        if (response.data.code === 200) {
          selectedGroup.value = null;
          await fetchGroups();
          alert('群组删除成功');
        } else {
          alert('删除群组失败: ' + response.data.msg);
        }
      } catch (error) {
        console.error('删除群组失败:', error);
        alert('删除群组失败: ' + (error.response?.data?.msg || error.message));
      }
    };
    
    const addUserToGroup = async () => {
      if (!selectedGroup.value || !newUserName.value) {
        alert('请输入用户名');
        return;
      }
      
      try {
        const response = await axios.post(`/api/group/${selectedGroup.value.groupId}/add-user-by-username`, {
          userName: newUserName.value,
          operatorUserId: parseInt(userId.value) // 添加操作者ID
        });
        
        if (response.data.code === 200) {
          // 刷新成员列表
          const membersResponse = await axios.get(`/api/group/${selectedGroup.value.groupId}/users`);
          if (membersResponse.data.code === 200) {
            groupMembers.value = membersResponse.data.data;
          }
          newUserName.value = '';
          showAddUserModal.value = false;
          alert('添加成员成功');
        } else {
          alert('添加成员失败: ' + response.data.msg);
        }
      } catch (error) {
        console.error('添加成员失败:', error);
        alert('添加成员失败: ' + (error.response?.data?.msg || error.message));
      }
    };
    
    const removeUserFromGroup = async (member) => {
      if (!selectedGroup.value || !member) return;
      
      try {
        // 检查当前用户权限
        if (!isCurrentUserAdmin.value) {
          alert('无权限，只有管理员或群主可以移除成员');
          return;
        }

        console.log('移除成员前的数据:', {
          selectedGroup: selectedGroup.value,
          member: member,
          memberId: member.userId,
          operatorId: userId.value,
          isAdmin: isCurrentUserAdmin.value
        });
        
        // 确保参数是数字类型
        const groupId = parseInt(selectedGroup.value.groupId);
        const memberId = parseInt(member.userId);
        const operatorId = parseInt(userId.value);
        
        console.log('转换后的参数:', {
          groupId: groupId,
          memberId: memberId,
          operatorId: operatorId,
          isNaNGroupId: isNaN(groupId),
          isNaNMemberId: isNaN(memberId),
          isNaNOperatorId: isNaN(operatorId)
        });
        
        if (isNaN(groupId) || isNaN(memberId) || isNaN(operatorId)) {
          alert('参数错误：群组ID或用户ID无效');
          return;
        }

        // 不能移除群主
        if (member.role === 'master') {
          alert('不能移除群主');
          return;
        }
        
        // 修改请求格式
        const response = await axios({
          method: 'delete',
          url: `/api/group/${groupId}/remove-user/${memberId}`,
          params: {
            operatorUserId: operatorId
          }
        });
        
        console.log('移除成员响应:', response.data);
        
        if (response.data.code === 200) {
          // 刷新成员列表
          const membersResponse = await axios.get(`/api/group/${groupId}/users`);
          if (membersResponse.data.code === 200) {
            groupMembers.value = membersResponse.data.data;
          }
          alert('移除成员成功');
        } else {
          alert('移除成员失败: ' + response.data.msg);
        }
      } catch (error) {
        console.error('移除成员失败:', {
          error: error,
          response: error.response?.data,
          status: error.response?.status,
          statusText: error.response?.statusText
        });
        alert('移除成员失败: ' + (error.response?.data?.msg || error.message));
      }
    };
    
    // 修改 fetchNotifications 函数
    const fetchNotifications = async () => {
      try {
        const res = await axios.get(`/api/notification/user/${userId.value}`);
        // 只显示未处理的通知
        notifications.value = res.data.filter(n => !n.isHandled);
        unreadNotifications.value = notifications.value.length;
        showNotifications.value = true;
      } catch (error) {
        showNotification('获取通知失败: ' + (error.response?.data?.msg || error.message), 'error');
      }
    };

    // 修改 acceptFriend 函数
    const acceptFriend = async (notificationId) => {
      try {
        await axios.post('/api/notification/accept-friend', { NotificationId: notificationId });
        showNotification('已同意好友请求，已创建私聊', 'success');
        await fetchNotifications(); // 立即刷新通知
        await fetchFriends(); // 立即刷新好友列表
        await updateUnreadNotifications(); // 更新未读通知数量
      } catch (error) {
        showNotification('操作失败: ' + (error.response?.data?.msg || error.message), 'error');
      }
    };

    // 修改 rejectFriend 函数
    const rejectFriend = async (notificationId) => {
      try {
        console.log('拒绝好友请求，通知ID:', notificationId);
        const response = await axios.post('/api/notification/reject-friend', { 
          notificationId: notificationId 
        });
        
        if (response.data && response.data.msg) {
          showNotification(response.data.msg, 'success');
        } else {
          showNotification('已拒绝好友请求', 'success');
        }
        
        // 立即刷新通知列表和未读数量
        await fetchNotifications();
        await updateUnreadNotifications();
      } catch (error) {
        console.error('拒绝好友请求失败:', error);
        showNotification('操作失败: ' + (error.response?.data?.msg || error.message), 'error');
      }
    };
    
    const confirmDeleteFriend = (friend) => {
      if (confirm('确定要删除这个好友吗？此操作不可恢复。')) {
        deleteFriend(friend);
      }
    };
    
    const deleteFriend = async (friend) => {
      try {
        const apiBaseUrl = window.apiBaseUrl || 'http://localhost:5067';
        const response = await axios.delete(`${apiBaseUrl}/api/notification/friend/${userId.value}/${friend.id}`);
        if (response.data && response.data.msg) {
          await fetchFriends();
          alert(response.data.msg);
        } else {
          alert('删除好友成功');
        }
      } catch (error) {
        console.error('删除好友失败:', error);
        alert('删除好友失败: ' + (error.response?.data?.msg || error.message));
      }
    };
    
    // 处理个人资料更新
    const handleProfileUpdated = (profileData) => {
      // 更新本地状态
      username.value = profileData.username || username.value;
      userAvatar.value = getFullAvatarUrl(profileData.avatar) || userAvatar.value;
      userSignature.value = profileData.signature || userSignature.value;
      
      // 关闭个人资料弹窗
      showProfileModal.value = false;
    };
    
    // 处理设置更新
    const handleSettingsUpdated = (settingsData) => {
      // 关闭设置弹窗
      showSettingsModal.value = false;
      
      // 应用设置
      // 如果需要立即应用其他设置，可以在此处添加代码
    };
    
    // 格式化通知时间
    const formatNotificationTime = (dateString) => {
      if (!dateString) return '';
      
      const date = new Date(dateString);
      const now = new Date();
      const diff = now.getTime() - date.getTime();
      
      // 如果小于24小时，显示x小时前
      if (diff < 24 * 60 * 60 * 1000) {
        const hours = Math.floor(diff / (60 * 60 * 1000));
        return hours > 0 ? `${hours}小时前` : '刚刚';
      }
      
      // 小于7天，显示x天前
      if (diff < 7 * 24 * 60 * 60 * 1000) {
        const days = Math.floor(diff / (24 * 60 * 60 * 1000));
        return `${days}天前`;
      }
      
      // 其他情况显示具体日期
      return date.toLocaleDateString('zh-CN');
    };
    
    // 页面加载时的初始化
    onMounted(() => {
      const token = localStorage.getItem('token');
      if (!token) {
        router.push('/login');
        return;
      }
      
      document.title = 'WHU-Chat | 主页';
      
      // 自动加载数据
      fetchFriends(); // 加载好友列表
      fetchGroups(); // 加载群组列表
      updateUnreadNotifications(); // 初始加载未读通知数量
      
      // 设置定时更新状态
      statusUpdateInterval = setInterval(updateFriendsOnlineStatus, 30000); // 每30秒更新一次
      
      // 设置定时更新未读通知数量
      const notificationInterval = setInterval(updateUnreadNotifications, 30000); // 每30秒更新一次
      
      // 在组件卸载时清除定时器
      onBeforeUnmount(() => {
        if (statusUpdateInterval) {
          clearInterval(statusUpdateInterval);
        }
        if (notificationInterval) {
          clearInterval(notificationInterval);
        }
      });
    });
    
    // 监听 activeSection 变化
    watch(activeSection, (newValue) => {
      if (newValue === 'groups') {
        fetchGroups();
      }
      if (newValue === 'friends') {
        fetchFriends();
      }
      if (newValue === 'chatrooms') {
        // 如果将来需要从后端加载聊天室数据，可以在这里添加逻辑
      }
    });
    
    const isCurrentUserAdmin = computed(() => {
      if (!selectedGroup.value || !userId.value) return false;
      const currentMember = groupMembers.value.find(m => parseInt(m.userId) === parseInt(userId.value));
      console.log('当前用户权限检查:', {
        currentMember,
        userId: userId.value,
        role: currentMember?.role,
        isAdmin: currentMember?.role === 'master' || currentMember?.role === 'admin'
      });
      return currentMember?.role === 'master' || currentMember?.role === 'admin';
    });
    
    const toggleAdminRole = async (member) => {
      if (!selectedGroup.value || !member) return;
      
      try {
        // 检查当前用户权限
        if (!isCurrentUserAdmin.value) {
          alert('无权限，只有管理员或群主可以切换角色');
          return;
        }

        const groupId = parseInt(selectedGroup.value.groupId);
        const targetUserId = parseInt(member.userId);
        const operatorId = parseInt(userId.value);
        
        console.log('切换管理员角色参数:', {
          groupId,
          targetUserId,
          operatorId,
          isNaNGroupId: isNaN(groupId),
          isNaNTargetUserId: isNaN(targetUserId),
          isNaNOperatorId: isNaN(operatorId)
        });
        
        if (isNaN(groupId) || isNaN(targetUserId) || isNaN(operatorId)) {
          alert('参数错误：群组ID或用户ID无效');
          return;
        }

        // 不能修改群主的角色
        if (member.role === 'master') {
          alert('不能修改群主的角色');
          return;
        }
        
        // 修改请求格式
        const response = await axios({
          method: 'post',
          url: `/api/group/${groupId}/toggle-admin/${targetUserId}`,
          params: {
            operatorUserId: operatorId
          }
        });
        
        console.log('切换管理员角色响应:', response.data);
        
        if (response.data.code === 200) {
          // 刷新成员列表
          const membersResponse = await axios.get(`/api/group/${groupId}/users`);
          if (membersResponse.data.code === 200) {
            groupMembers.value = membersResponse.data.data;
          }
          alert(response.data.msg || '操作成功');
        } else {
          alert(response.data.msg || '操作失败');
        }
      } catch (error) {
        console.error('切换管理员角色失败:', {
          error: error,
          response: error.response?.data,
          status: error.response?.status,
          statusText: error.response?.statusText
        });
        alert('切换管理员角色失败: ' + (error.response?.data?.msg || error.message));
      }
    };
    
    // 添加通知状态
    const notification = ref({
      show: false,
      message: '',
      type: 'info',
      timer: null
    });
    
    // 显示通知
    const showNotification = (message, type = 'info') => {
      // 清除之前的定时器
      if (notification.value.timer) {
        clearTimeout(notification.value.timer);
      }
      
      // 设置新的通知
      notification.value = {
        show: true,
        message,
        type,
        timer: setTimeout(() => {
          notification.value.show = false;
        }, 3000)
      };
    };
    
    return {
      // 用户信息
      userId,
      username,
      userAvatar,
      userSignature,
      
      // UI状态
      activeSection,
      searchQuery,
      showUserMenu,
      showProfileModal,
      showSettingsModal,
      showAddFriendModal,
      showCreateGroupModal,
      showOnlineFriends,
      showAddUserModal,
      showDeleteConfirm,
      
      // 表单数据
      friendUsername,
      groupId,
      groupName,
      groupDescription,
      
      // 搜索过滤
      friendSearch,
      groupSearch,
      
      // 通知计数
      chatroomNotifications,
      friendNotifications,
      groupNotifications,
      
      // 数据列表
      chatRooms,
      friendsList,
      groups,
      onlineFriends,
      filteredFriends,
      filteredGroups,
      
      // 群组详情相关
      selectedGroup,
      groupMembers,
      memberSearch,
      newUserName,
      filteredMembers,
      
      // 系统通知相关
      notifications,
      showNotifications,
      unreadNotifications,
      
      // 方法
      toggleUserMenu,
      enterChatRoom,
      openPrivateChat,
      openGroupChat,
      addFriend,
      createGroup,
      logout,
      navigateToAIChat,
      navigateToGroupChat,
      navigateToForums,
      navigateToMemberBenefits,
      selectGroup,
      confirmDeleteGroup,
      deleteGroup,
      addUserToGroup,
      removeUserFromGroup,
      fetchNotifications,
      acceptFriend,
      confirmDeleteFriend,
      deleteFriend,
      
      // 新增返回项
      handleProfileUpdated,
      handleSettingsUpdated,
      formatNotificationTime,
      isCurrentUserAdmin,
      toggleAdminRole,
      
      // 通知状态
      notification,
      showNotification,
      rejectFriend,
      updateUnreadNotifications,
    };
  }
};
</script>

<style scoped>
/* 基础样式 */
* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

.home-container {
  display: flex;
  height: 100vh;
  background-color: #f5f7fb;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
  overflow: hidden;
}

/* 侧边栏样式 */
.sidebar {
  width: 280px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  display: flex;
  flex-direction: column;
  transition: all 0.3s ease;
  box-shadow: 0 0 20px rgba(0, 0, 0, 0.1);
}

/* 用户资料部分 */
.user-profile {
  display: flex;
  align-items: center;
  padding: 20px;
  position: relative;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.avatar-container {
  position: relative;
}

.user-avatar, .user-avatar-placeholder {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background-color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #4776E6;
  font-weight: bold;
  font-size: 18px;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
  overflow: hidden;
}

.user-avatar img {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
}

.online-indicator {
  position: absolute;
  width: 12px;
  height: 12px;
  background-color: #4CD964;
  border-radius: 50%;
  border: 2px solid #4776E6;
  bottom: 0;
  right: 0;
}

.user-info {
  margin-left: 12px;
  flex: 1;
}

.username {
  font-weight: 600;
  font-size: 16px;
  margin-bottom: 2px;
}

.status {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.8);
}

.user-signature {
  font-size: 12px;
  color: rgba(255, 255, 255, 0.8);
  margin-top: 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 160px;
}

.user-menu {
  cursor: pointer;
  padding: 8px;
  border-radius: 50%;
  transition: background-color 0.2s;
  position: relative;
}

.user-menu:hover {
  background-color: rgba(255, 255, 255, 0.1);
}

.dropdown-menu {
  position: absolute;
  top: 40px;
  right: 0;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
  width: 160px;
  z-index: 10;
  overflow: hidden;
  animation: slideIn 0.2s ease;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.menu-item {
  padding: 12px 16px;
  display: flex;
  align-items: center;
  color: #333;
  transition: background-color 0.2s;
}

.menu-item i {
  margin-right: 10px;
  font-size: 14px;
  color: #8E54E9;
}

.menu-item:hover {
  background-color: #f5f5f5;
}

.menu-item.logout {
  border-top: 1px solid #eee;
  color: #ff4757;
}

.menu-item.logout i {
  color: #ff4757;
}

/* 导航菜单 */
.nav-menu {
  flex: 1;
  overflow-y: auto;
  padding: 15px;
}

.nav-menu::-webkit-scrollbar {
  width: 4px;
}

.nav-menu::-webkit-scrollbar-thumb {
  background-color: rgba(255, 255, 255, 0.2);
  border-radius: 4px;
}

.search-box {
  display: flex;
  align-items: center;
  background-color: rgba(255, 255, 255, 0.1);
  border-radius: 20px;
  padding: 8px 15px;
  margin-bottom: 20px;
  transition: all 0.3s ease;
}

.search-box:focus-within {
  background-color: rgba(255, 255, 255, 0.2);
  box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.3);
}

.search-box i {
  font-size: 14px;
  margin-right: 8px;
}

.search-box input {
  background: transparent;
  border: none;
  color: white;
  flex: 1;
  outline: none;
  font-size: 14px;
}

.search-box input::placeholder {
  color: rgba(255, 255, 255, 0.7);
}

.menu-section {
  margin-bottom: 25px;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
  padding: 0 5px;
  font-size: 13px;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: rgba(255, 255, 255, 0.7);
}

.section-header i {
  margin-right: 8px;
}

.add-button {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background-color: rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
  position: relative;
}

.add-button i {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  font-size: 12px;
  line-height: 1;
}

.add-button:hover {
  background-color: rgba(255, 255, 255, 0.2);
  transform: scale(1.1);
}

.nav-item {
  display: flex;
  align-items: center;
  padding: 12px 15px;
  border-radius: 8px;
  margin-bottom: 5px;
  cursor: pointer;
  transition: all 0.2s;
  position: relative;
}

.nav-item i {
  margin-right: 12px;
  font-size: 16px;
  width: 20px;
  text-align: center;
}

.nav-item:hover {
  background-color: rgba(255, 255, 255, 0.1);
}

.nav-item.active {
  background-color: rgba(255, 255, 255, 0.2);
  font-weight: 600;
}

.notification-badge {
  background-color: #FF3B30;
  color: white;
  border-radius: 10px;
  padding: 2px 6px;
  font-size: 11px;
  margin-left: auto;
  min-width: 18px;
  text-align: center;
}

.notification-badge.pulse {
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% {
    box-shadow: 0 0 0 0 rgba(255, 59, 48, 0.7);
  }
  70% {
    box-shadow: 0 0 0 6px rgba(255, 59, 48, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(255, 59, 48, 0);
  }
}

.expandable-section {
  margin-top: 10px;
}

.section-toggle {
  display: flex;
  align-items: center;
  padding: 8px 5px;
  cursor: pointer;
  font-size: 13px;
  color: rgba(255, 255, 255, 0.8);
}

.section-toggle i {
  margin-right: 5px;
  font-size: 10px;
  transition: transform 0.2s;
}

.friend-list {
  margin-top: 5px;
  padding-left: 10px;
}

.friend-item {
  display: flex;
  align-items: center;
  padding: 8px 10px;
  border-radius: 6px;
  margin-bottom: 2px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.friend-item:hover {
  background-color: rgba(255, 255, 255, 0.1);
}

.friend-avatar {
  position: relative;
  width: 32px;
  height: 32px;
  margin-right: 10px;
  border-radius: 50%;
  overflow: hidden;
  background-color: #f0f0f0;
}

.avatar-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.avatar-placeholder {
  width: 100%;
  height: 100%;
  background-color: #4776E6;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 14px;
}

.friend-status {
  position: absolute;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  bottom: 0;
  right: 0;
  border: 1px solid #4776E6;
}

.friend-status.online {
  background-color: #4CD964;
}

.friend-status.offline {
  background-color: #8A8A8E;
}

.friend-name {
  font-size: 13px;
}

.empty-list {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  padding: 15px;
  color: rgba(255, 255, 255, 0.5);
  font-size: 13px;
}

.empty-list i {
  font-size: 18px;
  margin-bottom: 5px;
}

/* 主内容区域 */
.main-content {
  flex: 1;
  overflow-y: auto;
  padding: 30px;
  background-color: #f5f7fb;
}

.main-content::-webkit-scrollbar {
  width: 6px;
}

.main-content::-webkit-scrollbar-thumb {
  background-color: #d1d1d1;
  border-radius: 3px;
}

.content-section {
  animation: fadeIn 0.4s ease;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.section-title {
  margin-bottom: 20px;
}

.section-title h2 {
  font-size: 24px;
  color: #333;
  margin-bottom: 5px;
  display: flex;
  align-items: center;
}

.section-title h2 i {
  margin-right: 12px;
  color: #4776E6;
}

.section-title p {
  color: #666;
  font-size: 14px;
}

/* 聊天室卡片 */
.rooms-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 20px;
}

.room-card {
  background-color: white;
  border-radius: 10px;
  padding: 20px;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.05);
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
}

.room-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
}

.room-header {
  display: flex;
  margin-bottom: 15px;
}

.room-icon {
  width: 50px;
  height: 50px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 15px;
  color: white;
  font-size: 20px;
}

.room-info {
  flex: 1;
}

.room-info h3 {
  font-size: 18px;
  font-weight: 600;
  color: #333;
  margin-bottom: 5px;
}

.room-stats {
  display: flex;
  font-size: 12px;
  color: #666;
}

.room-stats span {
  margin-right: 12px;
  display: flex;
  align-items: center;
}

.room-stats i {
  margin-right: 5px;
  color: #8E54E9;
}

.room-description {
  font-size: 14px;
  color: #666;
  line-height: 1.5;
  margin-bottom: 20px;
  flex: 1;
}

.room-footer {
  margin-top: auto;
}

.join-button {
  display: inline-flex;
  align-items: center;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  padding: 8px 16px;
  border-radius: 20px;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.2s;
}

.join-button i {
  margin-right: 6px;
  font-size: 12px;
}

.join-button:hover {
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
  transform: translateY(-2px);
}

/* 好友和群组部分 */
.friends-container, .groups-container {
  background-color: white;
  border-radius: 10px;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.05);
  overflow: hidden;
}

.friends-search, .groups-search {
  display: flex;
  padding: 15px 20px;
  border-bottom: 1px solid #eee;
}

.friends-search input, .groups-search input {
  flex: 1;
  border: 1px solid #e0e0e0;
  border-radius: 20px;
  padding: 8px 15px;
  outline: none;
  font-size: 14px;
  margin-right: 10px;
  transition: all 0.2s;
}

.friends-search input:focus, .groups-search input:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
}

.add-friend-btn, .create-group-btn {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  border-radius: 20px;
  padding: 8px 16px;
  font-size: 14px;
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: all 0.2s;
}

.add-friend-btn i, .create-group-btn i {
  margin-right: 6px;
  font-size: 12px;
}

.add-friend-btn:hover, .create-group-btn:hover {
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
  transform: translateY(-2px);
}

.friends-list, .groups-list {
  padding: 10px;
  max-height: 600px;
  overflow-y: auto;
}

.friends-list::-webkit-scrollbar, .groups-list::-webkit-scrollbar {
  width: 4px;
}

.friends-list::-webkit-scrollbar-thumb, .groups-list::-webkit-scrollbar-thumb {
  background-color: #d1d1d1;
  border-radius: 2px;
}

.friend-card, .group-card {
  display: flex;
  align-items: center;
  padding: 15px;
  border-radius: 8px;
  margin-bottom: 5px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.friend-card:hover, .group-card:hover {
  background-color: #f5f5f5;
}

.friend-avatar, .group-avatar {
  width: 45px;
  height: 45px;
  position: relative;
  margin-right: 15px;
}

.friend-details, .group-details {
  flex: 1;
}

.friend-details h3, .group-details h3 {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 2px;
  color: #333;
}

.friend-status-text {
  font-size: 12px;
  color: #666;
}

.friend-signature {
  font-size: 12px;
  color: #888;
  margin-top: 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 160px;
}

.group-stats {
  display: flex;
  font-size: 12px;
  color: #666;
}

.group-stats span {
  margin-right: 12px;
  display: flex;
  align-items: center;
}

.group-stats i {
  margin-right: 5px;
  color: #8E54E9;
}

.friend-actions, .group-actions {
  display: flex;
  gap: 10px;
}

.action-button {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background-color: #f0f0f0;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.action-button:hover {
  transform: translateY(-2px);
}

.action-button.chat {
  color: #4776E6;
}

.action-button.delete {
  color: #ff4757;
  background-color: #ffe0e3;
}

.empty-friends, .empty-groups {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
  text-align: center;
}

.empty-icon {
  width: 70px;
  height: 70px;
  background-color: #f5f5f5;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 15px;
  color: #8E54E9;
  font-size: 24px;
}

.empty-friends h3, .empty-groups h3 {
  font-size: 18px;
  margin-bottom: 5px;
  color: #333;
}

.empty-friends p, .empty-groups p {
  color: #666;
  margin-bottom: 15px;
  font-size: 14px;
}

/* 群组详情面板样式 */
.group-detail-panel {
  position: fixed;
  top: 0;
  right: 0;
  width: 400px;
  height: 100vh;
  background-color: white;
  box-shadow: -5px 0 15px rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
  z-index: 100;
  animation: slideIn 0.3s ease;
}

@keyframes slideIn {
  from {
    transform: translateX(100%);
  }
  to {
    transform: translateX(0);
  }
}

.panel-header {
  padding: 20px;
  border-bottom: 1px solid #eee;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.header-left {
  flex: 1;
  margin-right: 20px;
}

.header-left h3 {
  font-size: 20px;
  color: #333;
  margin-bottom: 8px;
}

.group-description {
  font-size: 14px;
  color: #666;
  line-height: 1.4;
}

.panel-content {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.group-info {
  display: flex;
  gap: 20px;
  margin-bottom: 20px;
  padding-bottom: 20px;
  border-bottom: 1px solid #eee;
}

.info-item {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #666;
  font-size: 14px;
}

.info-item i {
  color: #8E54E9;
}

.member-section {
  margin-bottom: 20px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

.section-header h4 {
  font-size: 16px;
  color: #333;
}

.member-search-box {
  display: flex;
  align-items: center;
  background-color: #f5f5f5;
  border-radius: 20px;
  padding: 6px 12px;
  width: 200px;
  border: 1px solid #e0e0e0;
}

.member-search-box i {
  color: #999;
  font-size: 14px;
  margin-right: 8px;
}

.member-search-box input {
  border: none;
  background: transparent;
  outline: none;
  font-size: 14px;
  width: 100%;
  color: #333;
  padding: 0;
}

.member-search-box input::placeholder {
  color: #999;
}

.member-search-box input:focus {
  outline: none;
}

.member-search-box:focus-within {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
}

.members {
  max-height: 300px;
  overflow-y: auto;
}

.member-item {
  display: flex;
  align-items: center;
  padding: 10px;
  border-radius: 8px;
  transition: background-color 0.2s;
}

.member-item:hover {
  background-color: #f5f5f5;
}

.member-avatar {
  width: 40px;
  height: 40px;
  margin-right: 12px;
}

.avatar-image {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
}

.member-info {
  flex: 1;
}

.member-name {
  font-size: 14px;
  color: #333;
  margin-right: 8px;
}

.member-role {
  font-size: 12px;
  color: #8E54E9;
  background-color: rgba(142, 84, 233, 0.1);
  padding: 2px 8px;
  border-radius: 10px;
  margin-left: 8px;
}

.member-role.admin {
  color: #4776E6;
  background-color: rgba(71, 118, 230, 0.1);
}

.member-actions {
  display: flex;
  gap: 8px;
  opacity: 0;
  transition: opacity 0.2s;
}

.member-item:hover .member-actions {
  opacity: 1;
}

.action-button.toggle-admin {
  color: #4776E6;
  background-color: #f0f7ff;
  width: 32px;
  height: 32px;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.action-button.remove {
  color: #ff4757;
  background-color: #ffe0e3;
  width: 32px;
  height: 32px;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.action-button.toggle-admin:hover {
  background-color: #e6f0ff;
}

.action-button.remove:hover {
  background-color: #ffd7dc;
}

.action-buttons {
  display: flex;
  gap: 10px;
  margin-top: auto;
  padding-top: 20px;
  border-top: 1px solid #eee;
}

.action-button {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 10px;
  border-radius: 8px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.action-button.add {
  background-color: #4776E6;
  color: white;
}

.action-button.delete {
  background-color: #ff4757;
  color: white;
}

.action-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
}

.action-button i {
  font-size: 14px;
}

.avatar-placeholder {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  background-color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #4776E6;
  font-weight: bold;
  font-size: 12px;
}

/* 通知列表样式 */
.notification-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.notification-item {
  padding: 15px;
  border-bottom: 1px solid #eee;
  transition: background-color 0.2s;
}

.notification-item:hover {
  background-color: #f5f5f5;
}

.notification-item.unread {
  background-color: #f0f7ff;
}

.notification-content {
  margin-bottom: 10px;
}

.notification-time {
  font-size: 12px;
  color: #999;
  text-align: right;
}

.empty-notification {
  padding: 20px;
  text-align: center;
  color: #999;
}

/* 好友请求通知样式 */
.friend-request-header {
  margin-bottom: 10px;
  font-weight: 500;
}

.friend-request-message {
  background-color: #f5f5f5;
  padding: 10px;
  border-radius: 8px;
  margin-bottom: 10px;
  font-style: italic;
  color: #666;
}

.friend-request-actions {
  display: flex;
  gap: 10px;
  margin-top: 10px;
}

.accept-button, .reject-button {
  padding: 6px 12px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  border: none;
  transition: all 0.2s;
}

.accept-button {
  background-color: #4776E6;
  color: white;
}

.reject-button {
  background-color: #f5f5f5;
  color: #666;
}

.accept-button:hover {
  background-color: #3b5dc4;
}

.reject-button:hover {
  background-color: #e9ecef;
}

.friend-request-status {
  font-size: 12px;
  color: #999;
  margin-top: 5px;
}

/* 系统通知模态窗口样式 */
.notification-modal-overlay {
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

.notification-modal-content {
  background-color: white;
  border-radius: 12px;
  width: 400px;
  max-width: 90%;
  max-height: 80vh;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
  animation: slideIn 0.3s ease;
}

.notification-modal-header {
  padding: 20px;
  border-bottom: 1px solid #eee;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.notification-modal-header h3 {
  margin: 0;
  font-size: 18px;
  color: #333;
  font-weight: 600;
}

.notification-modal-body {
  padding: 0;
  overflow-y: auto;
  max-height: calc(80vh - 70px);
}

.notification-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.notification-item {
  padding: 15px 20px;
  border-bottom: 1px solid #eee;
  transition: background-color 0.2s;
}

.notification-item:hover {
  background-color: #f5f5f5;
}

.notification-item.unread {
  background-color: #f0f7ff;
}

.notification-content {
  margin-bottom: 10px;
}

.notification-time {
  font-size: 12px;
  color: #999;
  text-align: right;
}

.empty-notification {
  padding: 30px 20px;
  text-align: center;
  color: #999;
  font-size: 14px;
}

.friend-request-header {
  font-weight: 500;
  margin-bottom: 8px;
  color: #333;
}

.friend-request-message {
  background-color: #f5f5f5;
  padding: 10px;
  border-radius: 8px;
  margin-bottom: 10px;
  font-style: italic;
  color: #666;
  font-size: 13px;
}

.friend-request-actions {
  display: flex;
  gap: 10px;
  margin-top: 10px;
}

.accept-button, .reject-button {
  padding: 6px 12px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  border: none;
  transition: all 0.2s;
}

.accept-button {
  background-color: #4776E6;
  color: white;
}

.reject-button {
  background-color: #f5f5f5;
  color: #666;
}

.accept-button:hover {
  background-color: #3b5dc4;
}

.reject-button:hover {
  background-color: #e9ecef;
}

.friend-request-status {
  font-size: 12px;
  color: #999;
  margin-top: 5px;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

@keyframes slideIn {
  from {
    transform: translateY(-20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.modal-overlay {
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
}

.modal-content {
  background-color: white;
  border-radius: 12px;
  width: 400px;
  max-width: 90%;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
  animation: modalSlideIn 0.3s ease;
}

@keyframes modalSlideIn {
  from {
    transform: translateY(-20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.modal-header {
  padding: 20px;
  border-bottom: 1px solid #e0e0e0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  margin: 0;
  font-size: 18px;
  color: #333;
  font-weight: 600;
}

.close-button {
  background: none;
  border: none;
  cursor: pointer;
  color: #666;
  font-size: 20px;
  padding: 5px;
  transition: all 0.2s;
}

.close-button:hover {
  color: #333;
  transform: rotate(90deg);
}

.modal-body {
  padding: 20px;
}

.create-group-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.form-group label {
  font-size: 14px;
  color: #333;
  font-weight: 500;
}

.form-input,
.form-textarea {
  padding: 10px 15px;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  transition: all 0.2s;
  width: 100%;
}

.form-textarea {
  min-height: 100px;
  resize: vertical;
}

.form-input:focus,
.form-textarea:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
  outline: none;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 10px;
}

.action-button {
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s;
}

.action-button.cancel {
  background-color: #f5f5f5;
  border: 1px solid #e0e0e0;
  color: #333;
}

.action-button.submit {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  border: none;
  color: white;
}

.action-button.cancel:hover {
  background-color: #e0e0e0;
}

.action-button.submit:hover {
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
}

.action-button i {
  font-size: 14px;
}

.add-member-section {
  position: fixed;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  background: white;
  border-radius: 12px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
  width: 400px;
  max-width: 90%;
  z-index: 1000;
  animation: modalFadeIn 0.3s ease;
}

@keyframes modalFadeIn {
  from {
    opacity: 0;
    transform: translate(-50%, -48%);
  }
  to {
    opacity: 1;
    transform: translate(-50%, -50%);
  }
}

.add-member-section .section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #eee;
}

.add-member-section .section-header h4 {
  margin: 0;
  font-size: 18px;
  color: #333;
  font-weight: 600;
}

.add-member-section .close-button {
  background: none;
  border: none;
  color: #666;
  cursor: pointer;
  padding: 8px;
  border-radius: 50%;
  transition: all 0.2s;
}

.add-member-section .close-button:hover {
  background: #f5f5f5;
  color: #333;
}

.add-member-form {
  padding: 20px;
}

.add-member-form .form-group {
  margin-bottom: 20px;
}

.add-member-form label {
  display: block;
  margin-bottom: 8px;
  color: #555;
  font-size: 14px;
  font-weight: 500;
}

.add-member-form input {
  width: 100%;
  padding: 12px;
  border: 2px solid #eee;
  border-radius: 8px;
  font-size: 14px;
  transition: all 0.2s;
}

.add-member-form input:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 3px rgba(71, 118, 230, 0.1);
  outline: none;
}

.add-member-form .form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
}

.add-member-form .action-button {
  padding: 10px 20px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s;
}

.add-member-form .action-button.cancel {
  background: #f5f5f5;
  border: none;
  color: #666;
}

.add-member-form .action-button.cancel:hover {
  background: #eee;
  color: #333;
}

.add-member-form .action-button.submit {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  border: none;
  color: white;
}

.add-member-form .action-button.submit:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(71, 118, 230, 0.2);
}

.add-member-form .action-button i {
  font-size: 14px;
}

/* 通知提示样式 */
.notification-toast {
  position: fixed;
  top: 20px;
  right: 20px;
  padding: 12px 20px;
  border-radius: 8px;
  background-color: white;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  display: flex;
  align-items: center;
  gap: 10px;
  z-index: 2000;
  min-width: 300px;
  max-width: 400px;
}

.notification-toast.success {
  background-color: #f0fdf4;
  border-left: 4px solid #22c55e;
  color: #166534;
}

.notification-toast.error {
  background-color: #fef2f2;
  border-left: 4px solid #ef4444;
  color: #991b1b;
}

.notification-toast.info {
  background-color: #f0f9ff;
  border-left: 4px solid #3b82f6;
  color: #1e40af;
}

.notification-toast i {
  font-size: 18px;
}

.notification-fade-enter-active,
.notification-fade-leave-active {
  transition: all 0.3s ease;
}

.notification-fade-enter-from,
.notification-fade-leave-to {
  opacity: 0;
  transform: translateX(30px);
}

/* 添加好友表单样式 */
.add-friend-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.add-friend-form .form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.add-friend-form label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  color: #333;
  font-weight: 500;
}

.add-friend-form label i {
  color: #4776E6;
}

.search-input-wrapper {
  position: relative;
  width: 100%;
}

.search-input {
  width: 100%;
  padding: 12px 40px 12px 15px;
  border: 2px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  transition: all 0.3s ease;
  background-color: #f8f9fa;
}

.search-input:focus {
  border-color: #4776E6;
  background-color: #fff;
  box-shadow: 0 0 0 3px rgba(71, 118, 230, 0.1);
  outline: none;
}

.search-icon {
  position: absolute;
  right: 15px;
  top: 50%;
  transform: translateY(-50%);
  color: #999;
  font-size: 14px;
  pointer-events: none;
}

.input-hint {
  font-size: 12px;
  color: #666;
  margin-top: 4px;
}

.add-friend-form .form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 10px;
}

.add-friend-form .action-button {
  padding: 10px 20px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  transition: all 0.3s ease;
}

.add-friend-form .action-button.cancel {
  background: #f5f5f5;
  border: none;
  color: #666;
}

.add-friend-form .action-button.cancel:hover {
  background: #eee;
  color: #333;
}

.add-friend-form .action-button.submit {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  border: none;
  color: white;
}

.add-friend-form .action-button.submit:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(71, 118, 230, 0.2);
}

.add-friend-form .action-button i {
  font-size: 14px;
}

/* 添加好友模态框样式 */
.add-friend-modal {
  background: linear-gradient(to bottom, #ffffff, #f8f9fa);
  border-radius: 16px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  animation: modalSlideIn 0.3s ease;
}

.add-friend-modal .modal-header {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  padding: 20px;
  color: white;
}

.add-friend-modal .modal-header h3 {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 20px;
  margin: 0;
}

.add-friend-modal .modal-header h3 i {
  font-size: 18px;
}

.add-friend-modal .close-button {
  color: white;
  opacity: 0.8;
  transition: all 0.3s ease;
}

.add-friend-modal .close-button:hover {
  opacity: 1;
  transform: rotate(90deg);
}

.add-friend-modal .modal-body {
  padding: 30px;
}

.add-friend-form {
  display: flex;
  flex-direction: column;
  gap: 25px;
}

.search-input-wrapper {
  position: relative;
  width: 100%;
  margin-bottom: 8px;
}

.search-input {
  width: 100%;
  padding: 15px 45px 15px 45px;
  border: 2px solid #e0e0e0;
  border-radius: 12px;
  font-size: 15px;
  transition: all 0.3s ease;
  background-color: white;
  color: #333;
}

.search-input:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 4px rgba(71, 118, 230, 0.1);
  outline: none;
}

.search-icon {
  position: absolute;
  left: 15px;
  top: 50%;
  transform: translateY(-50%);
  color: #4776E6;
  font-size: 16px;
  pointer-events: none;
}

.input-focus-border {
  position: absolute;
  bottom: 0;
  left: 0;
  width: 0;
  height: 2px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  transition: width 0.3s ease;
}

.search-input:focus + .input-focus-border {
  width: 100%;
}

.input-hint {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #666;
  margin-top: 8px;
  padding-left: 5px;
}

.input-hint i {
  color: #4776E6;
  font-size: 14px;
}

.add-friend-form .form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 15px;
  margin-top: 10px;
}

.add-friend-form .action-button {
  padding: 12px 24px;
  border-radius: 10px;
  font-size: 15px;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  transition: all 0.3s ease;
  border: none;
}

.add-friend-form .action-button.cancel {
  background: #f5f5f5;
  color: #666;
}

.add-friend-form .action-button.cancel:hover {
  background: #eee;
  color: #333;
  transform: translateY(-2px);
}

.add-friend-form .action-button.submit {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  box-shadow: 0 4px 15px rgba(71, 118, 230, 0.2);
}

.add-friend-form .action-button.submit:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(71, 118, 230, 0.3);
}

.add-friend-form .action-button i {
  font-size: 14px;
}

@keyframes modalSlideIn {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style> 