# 完整 SlideSLAM‑AirSim Container（兼容 legacy builder）
FROM nvidia/opengl:1.2-glvnd-devel-ubuntu20.04

ARG DEBIAN_FRONTEND=noninteractive
ENV TZ=Etc/UTC \
    ROS_DISTRO=noetic \
    NVIDIA_VISIBLE_DEVICES=all \
    NVIDIA_DRIVER_CAPABILITIES=all \
    DISPLAY=:0 \
    WORKSPACE=/root/slideslam_ws

# 1. 基础工具 & Python3 系统依赖（排除会冲突的 ROS Python 包）
RUN apt-get update && apt-get install -y --no-install-recommends \
      tzdata ca-certificates curl wget git vim nano sudo \
      lsb-release gnupg2 software-properties-common \
      build-essential cmake pkg-config \
      python3 python3-pip python3-dev python3-venv \
      python3-yaml python3-distutils python3-setuptools python3-pkg-resources \
      mesa-utils x11-apps unzip graphviz evince \
      libeigen3-dev libboost-all-dev libpcl-dev libgoogle-glog-dev \
      libdw-dev libtbb-dev libopencv-dev \
  && rm -rf /var/lib/apt/lists/*

# 2. 通过 pip 安装 ROS Python 工具和依赖
RUN pip3 install --no-cache-dir \
      catkin-tools catkin-pkg rospkg rosdistro \
      rosdep rosinstall rosinstall-generator wstool

# 3. 添加 ROS1 Noetic 源并安装 ROS 全家桶
RUN curl -sSL https://raw.githubusercontent.com/ros/rosdistro/master/ros.asc | apt-key add - && \
    echo "deb [arch=$(dpkg --print-architecture)] http://packages.ros.org/ros/ubuntu focal main" \
      > /etc/apt/sources.list.d/ros1.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends \
      ros-noetic-desktop-full \
      ros-noetic-tf2-ros ros-noetic-sensor-msgs ros-noetic-nav-msgs \
      ros-noetic-geometry-msgs ros-noetic-pcl-ros ros-noetic-cv-bridge \
      ros-noetic-image-transport ros-noetic-compressed-image-transport \
      ros-noetic-rviz ros-noetic-rqt ros-noetic-rqt-common-plugins \
  && rm -rf /var/lib/apt/lists/*

# 4. GTSAM（SLAM 后端）
RUN add-apt-repository -y ppa:borglab/gtsam-release-4.0 && \
    apt-get update && \
    apt-get install -y --no-install-recommends libgtsam-dev libgtsam-unstable-dev && \
    rm -rf /var/lib/apt/lists/*

# 5. Python 额外依赖（先安装基础包，再安装 AirSim 依赖包）
RUN python3 -m pip install --no-cache-dir \
      numpy==1.22.3 scikit-learn scipy open3d opencv-python \
      matplotlib ultralytics==8.0.59

# 安装 AirSim 依赖
RUN python3 -m pip install --no-cache-dir \
      msgpack msgpack-rpc-python && \
    python3 -m pip install --no-cache-dir \
      airsim \
      git+https://github.com/dimatura/pypcd.git

# 6. 初始化 rosdep（失败继续）
RUN rosdep init || true && rosdep update || true

# 7. 创建并切换到工作空间
RUN mkdir -p ${WORKSPACE}
WORKDIR ${WORKSPACE}

# 8. 自动 source ROS，每次 shell 都生效
RUN echo "source /opt/ros/noetic/setup.bash"   >> /root/.bashrc && \
    echo "export ROS_HOSTNAME=localhost"       >> /root/.bashrc && \
    echo "export ROS_MASTER_URI=http://localhost:11311" >> /root/.bashrc

# 9. AirSim 配置文件
RUN mkdir -p /root/Documents/AirSim
COPY settings.json /root/Documents/AirSim/settings.json

# 10. 拷贝启动脚本 & 授权 & 添加 alias
COPY scripts/*.sh /root/
RUN chmod +x /root/*.sh && \
    echo "alias setup_container='/root/setup_container.sh'"      >> /root/.bashrc && \
    echo "alias start_airsim='/root/start_airsim.sh'"            >> /root/.bashrc && \
    echo "alias launch_integration='/root/launch_integration.sh'">> /root/.bashrc && \
    echo "alias launch_coordination='/root/launch_coordination.sh'" >> /root/.bashrc && \
    echo "alias run_diagnostics='/root/run_diagnostics.sh'"      >> /root/.bashrc && \
    echo "alias open_rviz='/root/open_rviz.sh'"                  >> /root/.bashrc && \
    echo "alias workspace='cd /root/slideslam_ws/Source_Code/Resource-Aware-Coordination-AirSim/AirSim/ros'" >> /root/.bashrc

# 11. 暴露端口 & 默认命令
EXPOSE 11311 41451 9090
CMD ["/bin/bash"]
