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
               @click="activeSection = 'forums'" 
               :class="{ active: activeSection === 'forums' }">
            <i class="fa-solid fa-list-ul"></i>
            <span>讨论区</span>
            <div class="notification-badge pulse" v-if="forumNotifications > 0">{{ forumNotifications }}</div>
          </div>
          <div class="nav-item" 
               @click="navigateToAIChat()" 
               :class="{ active: false }">
            <i class="fa-solid fa-robot"></i>
            <span>AI咨询</span>
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
              <span>在线好友 ({{ onlineFriends.length }})</span>
            </div>
            <transition name="slide-fade">
              <div class="friend-list" v-if="showOnlineFriends">
                <div v-for="friend in onlineFriends" :key="friend.id" 
                     class="friend-item" @click="openPrivateChat(friend)">
                  <div class="friend-avatar">
                    <img v-if="friend.avatar" :src="friend.avatar" :alt="friend.username">
                    <div v-else class="avatar-placeholder">{{ friend.username.charAt(0).toUpperCase() }}</div>
                    <div class="friend-status online"></div>
                  </div>
                  <div class="friend-name">{{ friend.username }}</div>
                </div>
                <div class="empty-list" v-if="onlineFriends.length === 0">
                  <i class="fa-solid fa-user-slash"></i>
                  <span>暂无在线好友</span>
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
            <span>我的群组</span>
            <div class="notification-badge pulse" v-if="groupNotifications > 0">{{ groupNotifications }}</div>
          </div>
          <div class="nav-item" 
               @click="navigateToGroupChat()" 
               :class="{ active: false }">
            <i class="fa-solid fa-comments"></i>
            <span>群组聊天</span>
          </div>
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
                <img v-if="friend.avatar" :src="friend.avatar" :alt="friend.username">
                <div v-else class="avatar-placeholder">{{ friend.username.charAt(0).toUpperCase() }}</div>
                <div class="friend-status" :class="friend.status"></div>
              </div>
              <div class="friend-details">
                <h3>{{ friend.username }}</h3>
                <p class="friend-status-text">{{ friend.status === 'online' ? '在线' : '离线' }}</p>
              </div>
              <div class="friend-actions">
                <button class="action-button chat">
                  <i class="fa-solid fa-comment"></i>
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
          <h2><i class="fa-solid fa-users"></i> 我的群组</h2>
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
            <div v-for="group in filteredGroups" :key="group.id" 
                 class="group-card" @click="openGroupChat(group)">
              <div class="group-avatar">
                <img v-if="group.avatar" :src="group.avatar" :alt="group.name">
                <div v-else class="avatar-placeholder">{{ group.name.charAt(0).toUpperCase() }}</div>
              </div>
              <div class="group-details">
                <h3>{{ group.name }}</h3>
                <p class="group-stats">
                  <span><i class="fa-solid fa-user"></i> {{ group.memberCount }} 成员</span>
                  <span><i class="fa-solid fa-circle"></i> {{ group.onlineCount }} 在线</span>
                </p>
              </div>
              <div class="group-actions">
                <button class="action-button chat">
                  <i class="fa-solid fa-comments"></i>
                </button>
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

      <!-- 讨论区部分 -->
      <div v-if="activeSection === 'forums'" class="content-section forums-section">
        <div class="section-title">
          <h2><i class="fa-solid fa-list-ul"></i> 校园讨论区</h2>
          <p>浏览校园热门话题</p>
        </div>
        
        <div class="forums-container">
          <div class="forums-categories">
            <div v-for="category in forumCategories" :key="category.id"
                 class="category-card" @click="selectForumCategory(category)">
              <div class="category-icon">
                <i :class="category.icon"></i>
              </div>
              <div class="category-details">
                <h3>{{ category.name }}</h3>
                <p>{{ category.description }}</p>
              </div>
              <div class="category-stats">
                <span><i class="fa-solid fa-file-lines"></i> {{ category.postCount }} 帖子</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 模态窗口：添加好友 -->
    <div class="modal-overlay" v-if="showAddFriendModal" @click="showAddFriendModal = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>添加好友</h3>
          <button class="close-button" @click="showAddFriendModal = false">
            <i class="fa-solid fa-times"></i>
          </button>
        </div>
        <div class="modal-body">
          <div class="add-friend-form">
            <div class="form-group">
              <label for="friendUsername">用户名</label>
              <input type="text" id="friendUsername" v-model="friendUsername" placeholder="输入用户名...">
            </div>
            <button class="submit-button" @click="addFriend">
              <i class="fa-solid fa-user-plus"></i> 添加
            </button>
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
              <input type="text" id="groupName" v-model="groupName" placeholder="输入群组名称...">
            </div>
            <div class="form-group">
              <label for="groupDescription">群组描述</label>
              <textarea id="groupDescription" v-model="groupDescription" placeholder="输入群组描述..."></textarea>
            </div>
            <button class="submit-button" @click="createGroup">
              <i class="fa-solid fa-plus"></i> 创建
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';

export default {
  name: 'HomeView',
  setup() {
    const router = useRouter();
    
    // 用户信息
    const userId = ref(localStorage.getItem('userId') || '');
    const username = ref(localStorage.getItem('username') || '');
    const userAvatar = ref(localStorage.getItem('userAvatar') || '');
    
    // UI 状态
    const activeSection = ref('chatrooms');
    const searchQuery = ref('');
    const showUserMenu = ref(false);
    const showProfileModal = ref(false);
    const showSettingsModal = ref(false);
    const showAddFriendModal = ref(false);
    const showCreateGroupModal = ref(false);
    const showOnlineFriends = ref(true);
    
    // 表单数据
    const friendUsername = ref('');
    const groupName = ref('');
    const groupDescription = ref('');
    
    // 搜索过滤
    const friendSearch = ref('');
    const groupSearch = ref('');
    
    // 通知计数
    const chatroomNotifications = ref(0);
    const friendNotifications = ref(0);
    const groupNotifications = ref(2); // 示例数据
    const forumNotifications = ref(5); // 示例数据
    
    // 模拟数据 - 实际项目中应从API获取
    const chatRooms = ref([
      {
        id: 1,
        name: 'WHU 校园公共聊天室',
        description: '欢迎来到武汉大学校园公共聊天室，这里是交流分享的空间！',
        icon: 'fa-solid fa-school',
        onlineCount: 24,
        messageCount: 1253
      },
      {
        id: 2,
        name: '计算机学院交流群',
        description: '计算机学院同学技术交流、学习讨论的地方',
        icon: 'fa-solid fa-laptop-code',
        onlineCount: 12,
        messageCount: 856
      },
      {
        id: 3,
        name: '校园活动通知',
        description: '发布和讨论校园各类活动的聊天室',
        icon: 'fa-solid fa-calendar-days',
        onlineCount: 8,
        messageCount: 432
      }
    ]);
    
    const friends = ref([
      {
        id: 101,
        username: '张三',
        status: 'online',
        avatar: null
      },
      {
        id: 102,
        username: '李四',
        status: 'offline',
        avatar: null
      },
      {
        id: 103,
        username: '王五',
        status: 'online',
        avatar: null
      }
    ]);
    
    const groups = ref([
      {
        id: 201,
        name: '软件工程课题组',
        memberCount: 12,
        onlineCount: 5,
        avatar: null
      },
      {
        id: 202,
        name: '毕业设计小组',
        memberCount: 8,
        onlineCount: 3,
        avatar: null
      }
    ]);
    
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
      return friends.value.filter(friend => friend.status === 'online');
    });
    
    const filteredFriends = computed(() => {
      if (!friendSearch.value) return friends.value;
      return friends.value.filter(friend => 
        friend.username.toLowerCase().includes(friendSearch.value.toLowerCase())
      );
    });
    
    const filteredGroups = computed(() => {
      if (!groupSearch.value) return groups.value;
      return groups.value.filter(group => 
        group.name.toLowerCase().includes(groupSearch.value.toLowerCase())
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
      // 实际应用中应先创建或获取私聊会话ID
      console.log(`打开与 ${friend.username} 的私聊`);
    };
    
    const openGroupChat = (group) => {
      // 实际应用中应导航到群聊界面
      console.log(`打开群聊 ${group.name}`);
    };
    
    const selectForumCategory = (category) => {
      // 实际应用中应导航到论坛分类页面
      console.log(`选择论坛分类: ${category.name}`);
    };
    
    const addFriend = async () => {
      if (!friendUsername.value) return;
      
      try {
        // 这里应该调用实际的API
        console.log(`添加好友: ${friendUsername.value}`);
        // 添加成功后清空输入并关闭模态窗口
        friendUsername.value = '';
        showAddFriendModal.value = false;
        
        // 这里可以添加成功提示
      } catch (error) {
        console.error('添加好友失败:', error);
      }
    };
    
    const createGroup = async () => {
      if (!groupName.value) return;
      
      try {
        // 这里应该调用实际的API
        console.log(`创建群组: ${groupName.value}`);
        // 创建成功后清空输入并关闭模态窗口
        groupName.value = '';
        groupDescription.value = '';
        showCreateGroupModal.value = false;
        
        // 这里可以添加成功提示
      } catch (error) {
        console.error('创建群组失败:', error);
      }
    };
    
    const logout = () => {
      // 清除localStorage中的用户信息
      localStorage.removeItem('token');
      localStorage.removeItem('userId');
      localStorage.removeItem('username');
      localStorage.removeItem('userAvatar');
      
      // 跳转到登录页
      router.push('/login');
    };
    
    // 导航到AI聊天页面
    const navigateToAIChat = () => {
      router.push('/chat');
    };
    
    // 导航到群组聊天页面
    const navigateToGroupChat = () => {
      router.push('/groupchat');
    };
    
    // 页面加载时的初始化
    onMounted(() => {
      // 检查是否已登录
      const token = localStorage.getItem('token');
      if (!token) {
        router.push('/login');
        return;
      }
      
      // 设置页面标题
      document.title = 'WHU-Chat | 主页';
      
      // 实际应用中应该从后端获取数据
      // 这里可以添加API调用以获取聊天室、好友、群组等数据
    });
    
    return {
      // 用户信息
      userId,
      username,
      userAvatar,
      
      // UI状态
      activeSection,
      searchQuery,
      showUserMenu,
      showProfileModal,
      showSettingsModal,
      showAddFriendModal,
      showCreateGroupModal,
      showOnlineFriends,
      
      // 表单数据
      friendUsername,
      groupName,
      groupDescription,
      
      // 搜索过滤
      friendSearch,
      groupSearch,
      
      // 通知计数
      chatroomNotifications,
      friendNotifications,
      groupNotifications,
      forumNotifications,
      
      // 数据列表
      chatRooms,
      friends,
      groups,
      forumCategories,
      onlineFriends,
      filteredFriends,
      filteredGroups,
      
      // 方法
      toggleUserMenu,
      enterChatRoom,
      openPrivateChat,
      openGroupChat,
      selectForumCategory,
      addFriend,
      createGroup,
      logout,
      navigateToAIChat,
      navigateToGroupChat
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
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background-color: rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
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
}

.friend-avatar img, .avatar-placeholder {
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
}

.action-button {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #f5f5f5;
  color: #4776E6;
  border: none;
  cursor: pointer;
  transition: all 0.2s;
}

.action-button:hover {
  background-color: #4776E6;
  color: white;
  transform: scale(1.1);
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

/* 论坛分类卡片 */
.forums-categories {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 20px;
}

.category-card {
  background-color: white;
  border-radius: 10px;
  padding: 20px;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.05);
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
}

.category-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
}

.category-icon {
  width: 50px;
  height: 50px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 15px;
  color: white;
  font-size: 20px;
}

.category-details h3 {
  font-size: 18px;
  font-weight: 600;
  color: #333;
  margin-bottom: 8px;
}

.category-details p {
  font-size: 14px;
  color: #666;
  margin-bottom: 15px;
  flex: 1;
}

.category-stats {
  color: #666;
  font-size: 13px;
  display: flex;
  align-items: center;
}

.category-stats i {
  margin-right: 5px;
  color: #8E54E9;
}

/* 模态窗口 */
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
  z-index: 100;
  animation: fadeIn 0.2s ease;
}

.modal-content {
  width: 400px;
  background-color: white;
  border-radius: 10px;
  overflow: hidden;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
  animation: scaleIn 0.3s ease;
}

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

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px;
  border-bottom: 1px solid #eee;
}

.modal-header h3 {
  font-size: 18px;
  color: #333;
}

.close-button {
  background: none;
  border: none;
  color: #666;
  font-size: 18px;
  cursor: pointer;
  transition: color 0.2s;
}

.close-button:hover {
  color: #ff4757;
}

.modal-body {
  padding: 20px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  font-size: 14px;
  color: #333;
  font-weight: 500;
}

.form-group input, .form-group textarea {
  width: 100%;
  padding: 10px 15px;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  outline: none;
  transition: all 0.2s;
}

.form-group input:focus, .form-group textarea:focus {
  border-color: #4776E6;
  box-shadow: 0 0 0 2px rgba(71, 118, 230, 0.1);
}

.form-group textarea {
  min-height: 100px;
  resize: vertical;
}

.submit-button {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  border-radius: 8px;
  padding: 12px;
  width: 100%;
  font-size: 16px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.submit-button i {
  margin-right: 8px;
}

.submit-button:hover {
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
  transform: translateY(-2px);
}

/* 过渡动画 */
.slide-fade-enter-active, .slide-fade-leave-active {
  transition: all 0.3s ease;
}

.slide-fade-enter-from, .slide-fade-leave-to {
  transform: translateY(-10px);
  opacity: 0;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .home-container {
    flex-direction: column;
  }
  
  .sidebar {
    width: 100%;
    height: 60px;
    flex-direction: row;
  }
  
  .main-content {
    padding: 15px;
  }
  
  .rooms-grid, .forums-categories {
    grid-template-columns: 1fr;
  }
}
</style> 